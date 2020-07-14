#!/usr/bin/env bash

# Check if topology file and pcap directory are passed as command line arguments
if [[ -z "$1" ]]||[[ -z "$2" ]]
then
  echo "Usage: ./traffic.sh <topology file path> <pacp directory path>"
  exit
fi

# Access topology file from command line arguments
TOPO_FILE=$1
if [ -f ${TOPO_FILE} ]
then
  echo "Using TOPOLOGY file = ${TOPO_FILE}"
else
  echo "${TOPO_FILE} file does not exist"
  exit
fi

# Access pcap directory from command line arguments
PCAP_DIR=$2
if [ -d ${PCAP_DIR} ] 
then
  # If there is a "/" at the end then remove it
  if [ ${PCAP_DIR:length-1} = "/" ]
  then
    PCAP_DIR=${PCAP_DIR/%"/"/""}
  fi
  echo "Using PCAP directory = ${PCAP_DIR}"
else
  echo "${PCAP_DIR} directory does not exist"
  exit
fi

# [python script] Reading topology file and find out the list of supporting devices
function read_topology {
PYTHON_ARG="$1" python - <<END

import os
import sys
import yaml
from collections import defaultdict
from argparse import ArgumentParser

topo = yaml.safe_load(open(os.environ['PYTHON_ARG']))
s_h_links = defaultdict(set)
s_names = set()

for host, info in topo['hosts'].items():
    for interface in info.get('interfaces', []):
        if 'link' in interface:
            s_h_links[host].add(interface['link'])
            s_h_links[interface['link']].add(host)

for switch, info in topo['switches'].items():
    for interface in info.get('interfaces', []):
        if 'link' in interface:
            s_h_links[switch].add(interface['link'])
            s_h_links[interface['link']].add(switch)
    s_names.add(switch)

for node, links in s_h_links.items():
    if node in s_names and len(links)==1:
        print(node),

END
}

satellites=$(read_topology ${TOPO_FILE})
sat_array=(${satellites// / })

# Removing existing output folder and create new one
OUTPUT=output
if [ -d ${OUTPUT} ] 
then
	rm -r ${OUTPUT}
fi
mkdir ${OUTPUT}

# Creating empty intermediate time dump file
TIME_DUMP=${OUTPUT}/time_dump_log.txt
echo "Using TIME_DUMP = ${TIME_DUMP}"
touch ${TIME_DUMP}
> ${TIME_DUMP}

# Creating empty temp file
TEMP=${OUTPUT}/temp.txt
echo "Using TEMP = ${TEMP}"
touch ${TEMP}
> ${TEMP}

# Create file to keep interval of packets (in microseconds)
TIME_STREAM=${OUTPUT}/time_stream.txt
echo "Using TIME_STREAM = ${TIME_STREAM}"
touch ${TIME_STREAM}

# Extracting src and dst of packet from pcap file name
# Extracting time stamps from pcap files
# Storing the above information in following structure [time_stamp src dst]
for pcap_file in "${PCAP_DIR}"/*; do
  # Extracting src and dst of packet from pcap file name
  src_dest=${pcap_file##*/}
  src_dest=${src_dest%.pcap}
  src_dest=${src_dest//"_to_"/" "}
  nodes=(${src_dest// / })

  # Take the tcp dump to temp file
  tcpdump -x -r ${pcap_file} > ${TEMP}

  # Store the formated output to the temp file
  # fourth column is a unique identifier of the packet 
  # containing the 22th-33rd byte of the packet, but excluding the 23rd and 24th bytes
  # If the pacap file involves supporting devices (satellites) then, discart first 36 bytes containing flightplan header
  # Formate of each row in temp file is as follows:
  # time_stamp source target packet_identifier
  is_sat="0"
  if [[ " ${sat_array[@]} " =~ " ${nodes[0]} " ]] || [[ " ${sat_array[@]} " =~ " ${nodes[1]} " ]] 
  then
    is_sat="1"
  else
    is_sat="0"
  fi

  awk -v ARG="${src_dest},${is_sat}" '{ 
    split(ARG, args, /[,]/)
    if(substr($1,1,2)!="0x") { 
      printf "%s %s ", $1, args[1]
    }
    if(args[2]=="0"){
      if(substr($1,1,7)=="0x0000:"){
        printf "%s%s", $8, $9
      }
      if(substr($1,1,7)=="0x0010:"){
        printf " %s%s ", $2, $3
        printf "%s0000%s%s%s%s", substr($4,3,2), $6, $7, $8, $9
      }
      if(substr($1,1,7)=="0x0020:"){
        printf "%s\n", substr($2,1,2)
      }
    }
    if(args[2]=="1"){
      if(substr($1,1,7)=="0x0030:"){
        printf "%s%s %s%s ", $2, $3, $4, $5
        printf "%s0000%s%s", substr($6,3,2), $8, $9
      }
      if(substr($1,1,7)=="0x0040:"){
        printf "%s%s%s\n", $2, $3, substr($4,1,2)
      }
    }
  }' ${TEMP} > ${TIME_DUMP}

  > ${TEMP}

  # Merge the existing data with the new pcapfile data in sorted order based upon time stamp
  sort ${TIME_DUMP} ${TIME_STREAM} > ${TEMP}
  cat ${TEMP} > ${TIME_STREAM}
done

# Store the final data into time_dump_log.txt file
cat ${TIME_STREAM} > ${TIME_DUMP}
> ${TIME_STREAM}

# First packet time is a reference time to calculate the elapsed time of all the packets
time=$(head -n 1 ${TIME_DUMP})
readarray -d " " -t time_stream <<< ${time}

# FInding out elapsed time and hash of each packet
echo "Finding hash and elapsed time of each packet..."
awk -v BASE="${time_stream[0]}" '{ 
  cmd="echo -n " $6 " | md5sum|cut -d\" \" -f1"; cmd|getline hash;
  close(cmd)
  split(BASE, base_time_array, /[:.]/)
  split($1, time_array, /[:.]/) 
  elapsed_time=(time_array[1] - base_time_array[1])*60*60*1000*1000 + (time_array[2] - base_time_array[2])*60*1000*1000 + (time_array[3] - base_time_array[3])*1000*1000 + time_array[4] - base_time_array[4]
  print elapsed_time " " $2 " " $3 " " $4 " " $5 " " hash
}' ${TIME_DUMP} > ${TIME_STREAM}

rm -r ${TEMP}

echo "Operation Completed."
