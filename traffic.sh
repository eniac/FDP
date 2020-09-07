#!/usr/bin/env bash

# Check if topology file and pcap directory are passed as command line arguments
if [[ -z "$1" ]]||[[ -z "$2" ]]||[[ -z "$3" ]] ||[[ -z "$4" ]]
then
  echo "Usage: ./traffic.sh <topology file path> <pacp directory path> <StreamingAssets directory path> <name of experiment>"
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

# Access StreamingAssets Directory location
STREAMING_ASSET=$3
if [ -d ${STREAMING_ASSET} ] 
then
# If there is a "/" at the end then remove it
  if [ ${STREAMING_ASSET:length-1} = "/" ]
  then
    STREAMING_ASSET=${STREAMING_ASSET/%"/"/""}
  fi
  echo "Using PCAP directory = ${STREAMING_ASSET}"
else
  echo "${STREAMING_ASSET} directory does not exist"
  exit
fi

# Create a file to use as Experiment files info and append the name of new experiment if not exist
EXPERIMENTS=${STREAMING_ASSET}/experiments.txt
echo "Using TOPOLOGY file = ${TOPO_FILE}"
if [ ! -f ${TOPO_FILE} ]
then
  touch ${EXPERIMENTS}
fi
if ! grep -Fxq "$4" ${EXPERIMENTS}
then
  echo $4 >> ${EXPERIMENTS}
fi


# Create an output directory to keep the output file
OUTPUT=${STREAMING_ASSET}"/"$4
echo "Using TEST directory = ${OUTPUT}"
# Removing existing output folder and create new one
if [ -d ${OUTPUT} ] 
then
  rm -r ${OUTPUT}
fi
mkdir ${OUTPUT}


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
METADATA=${OUTPUT}/metadata.txt
echo "Using METADATA = ${METADATA}"
touch ${METADATA}

# Create a file to keep topology 
TOPO=${OUTPUT}/topology.yml
echo "Using TOPOLOGY = ${TOPO}"
touch ${TOPO}
cat ${TOPO_FILE} > ${TOPO}


# Extracting src and dst of packet from pcap file name
# Extracting time stamps from pcap files
# Storing the above information in following structure [time_stamp src dst]
for pcap_file in "${PCAP_DIR}"/*; do
  # Extracting src and dst of packet from pcap file name
  if [[ ${pcap_file} != *"_to_"* ]]
  then
    continue
  fi
  src_dest=${pcap_file##*/}
  src_dest=${src_dest%.pcap}
  src_dest=${src_dest//"_to_"/" "}
  nodes=(${src_dest// / })

  # Take the tcp dump to temp file
  tcpdump -xx -r ${pcap_file} > ${TEMP}

 # 13 to 33 skip 21 23 24
  # Store the formated output to the temp file
  # fourth column is a unique identifier of the packet 
  # containing the 22th-33rd byte of the packet, but excluding the 23rd and 24th bytes
  # If the pacap file involves supporting devices (satellites) then, discart first 36 bytes containing flightplan header
  # Formate of each row in temp file is as follows:
  # time_stamp source target packet_identifier
  is_sat="0"
  # if [[ " ${sat_array[@]} " =~ " ${nodes[0]} " ]] || [[ " ${sat_array[@]} " =~ " ${nodes[1]} " ]] 
  if [[ " ${sat_array[@]} " =~ " ${nodes[0]} " ]]
  then
    is_sat="1"
  else
    is_sat="0"
  fi

  awk -v ARG="${src_dest},${is_sat}" '{ 
    split(ARG, args, /[,]/)
    if(/length/){
      # printf "%s %s ", $1,args[1]
      head = $1" "args[1]" "
      getline;
      if($8 == "88cc"){
        # Broadcast packet - ignore
      }
      else if($8 == "0800"){
        printf "%s", head
        # Full IP header
        getline;

        # ID from IP header
        lID = $3

        # Higher level protocol header (TCP/UDP/ICMP)
        protocol = substr($5,3,2)

        # Origin and destination
        src=$7$8
        d=$9
        getline
        dest=d""$2

        # UDP(MEMCACHE) header
        if(protocol == "11"){
          lastIDf = lID
          if($6=="0000"){
            printf "%s %s %s MCDC\n", lastIDf, src, dest  
          }
          else{
            printf "%s %s %s MCD\n", lastIDf, src, dest
          }
        }

        # ICMP header
        else if(protocol == "01"){
          lastIDf = lID
          printf "%s %s %s ICMP\n", lastIDf, src, dest
        }

        # TCP header
        else{
          getline
          lastIDf = lID""$3
          printf "%s %s %s TCP\n", lastIDf, src, dest
        }
      }

      # FEC header ahead
      else if($8 == "081c"){
        printf "%s", head
        getline;

        # Full IP header ahead
        if($2 == "0800"){
          
          # ID from IP header
          lID = $6

          # Higher level protocol header (TCP/UDP/ICMP)
          protocol = substr($8,3,2)

          # Origin and destination
          getline
          src=$2$3
          dest=$4$5

          # UDP(MEMCACHE) header
          if(protocol == "11"){
            lastID = lID
            if($9=="0000"){
              printf "%s %s %s MCDC\n", lastID, src, dest
            }
            else{
              printf "%s %s %s MCD\n", lastID, src, dest
            }
          }

          # ICMP header
          else if(protocol == "01"){
            lastID = lID
            printf "%s %s %s ICMP\n", lastID, src, dest
          }

          # TCP header
          else{
            getline
            lastID = lID""$6
            printf "%s %s %s TCP\n", lastID, src, dest
          }
        }

        # Compressed IP header ahead
        else if($2 == "1234"){
          lID = $6
          lastID = lID""$9
          printf "%s 00000000 00000000 HC\n", lastID
          
        }

        # Parity packet header ahead
        else if($2 == "081c"){
          printf "%s 00000000 00000000 PR\n", lastID
        }

        # Unknown header ahead
        else{
          lastID = "Unknown"
          print "Unknown 00000000 00000000 NO"
        }
      }

      # Compressed Header ahead
      else if($8 == "1234"){
        printf "%s", head
        getline

        lID = $3
        lastIDf = lID""$6
        printf "%s 00000000 00000000 HC\n", lastIDf
      }

      # Flightplan header
      else{
        state = substr($9,3,2)
        nak = 0
        # Additional packet with ACK/NAK set in flightplan header 
        if(args[2] == "1" && (state != "00")) {
          nak = 1
        }

        getline
        getline
        getline

        # Broadcast packet
        if($2 == "08cc"){
          # Broadcast packet - ignore
        }

        # IP header
        else if($2 == "0800"){

          printf "%s", head

          # ID from IP header
          lID =$5 

          # Higher level protocol header (TCP/UDP/ICMP)
          protocol = substr($7,3,2)

          # Origin and destination
          sr=$9
          getline
          src=sr""$2
          dest=$3$4

          # TCP header
          if(protocol == "06"){
            getline
            lastIDf = lID""$5
            if(nak == 1){
              printf "%s %s %s NAK\n", lastIDf, src, dest
            }
            else{
              printf "%s %s %s TCP\n", lastIDf, src, dest
            }
          }

          # ICMP protocol
          else if(protocol == "01"){
            lastIDf = lID
            if(nak == 1){
              printf "%s %s %s NAK\n", lastIDf, src, dest
            }
            else{
              printf "%s %s %s ICMP\n", lastIDf, src, dest
            }
          }

          # UDP(MEMCACHE) header
          else if(protocol == "11"){
            lastID = lID
            if(nak == 1){
              printf "%s %s %s NAK\n", lastID, src, dest
            }
            else if($8=="0000"){
              printf "%s %s %s MCDC\n", lastID, src, dest
            }
            else{
              printf "%s %s %s MCD\n", lastID, src, dest
            }
          }
        }

        # FEC header
        else if($2 == "081c"){

          printf "%s", head

          # Full IP header ahead
          if($4 == "0800"){
            
            # ID from IP header
            lID = $8

            # Higher level protocol header (TCP/UDP/ICMP)
            getline
            protocol = substr($2,3,2)

            # Origin and destination
            src=$4$5
            dest=$6$7

            # UDP(MEMCACHE) header
            if(protocol == "11"){
              lastID = lID
              getline
              if(nak == 1){
                printf "%s %s %s NAK\n", lastID, src, dest
              }
              else if($3=="0000"){
                printf "%s %s %s MCDC\n", lastID, src, dest
              }
              else{
                printf "%s %s %s MCD\n", lastID, src, dest
              }
            }

            # ICMP header
            else if(protocol == "01"){
              lastID = lID
              if(nak == 1){
                printf "%s %s %s NAK\n", lastID, src, dest
              }
              else{
                printf "%s %s %s ICMP\n", lastID, src, dest
              }
            }

            # TCP header
            else if(protocol == "06"){
              getline
              lastID = lID""$8
              if(nak == 1){
                printf "%s %s %s NAK\n", lastID, src, dest  
              }
              else{
                printf "%s %s %s TCP\n", lastID, src, dest
              }
            }
          }

          # Compressed IP+TCP header ahead
          else if($4 == "1234"){
            lID = $8
            getline
            lastID = lID""$3
            if(nak == 1){
              printf "%s 00000000 00000000 NAK\n", lastID
            }
            else{
              printf "%s 00000000 00000000 HC\n", lastID
            }
          }

          # Parity
          else if($4 == "081c"){
            if(nak == 1){
              printf "%s 00000000 00000000 NAK\n", lastID
            }
            else{
              printf "%s 00000000 00000000 PR\n", lastID
            }
          }
        }
      }
    }
  }' ${TEMP} > ${TIME_DUMP}

  > ${TEMP}

  # Merge the existing data with the new pcapfile data in sorted order based upon time stamp
  sort ${TIME_DUMP} ${METADATA} > ${TEMP}
  cat ${TEMP} > ${METADATA}
done

# Store the final data into time_dump_log.txt file
cat ${METADATA} > ${TIME_DUMP}
> ${METADATA}

# First packet time is a reference time to calculate the elapsed time of all the packets
time=$(head -n 1 ${TIME_DUMP})
readarray -d " " -t time_stream <<< ${time}

# FInding out elapsed time and hash of each packet
echo "Finding hash and elapsed time of each packet..."
awk -v BASE="${time_stream[0]}" '{ 
  cmd="echo -n " $4 " | md5sum|cut -d\" \" -f1"; cmd|getline hash;
  close(cmd)
  split(BASE, base_time_array, /[:.]/)
  split($1, time_array, /[:.]/) 
  elapsed_time=(time_array[1] - base_time_array[1])*60*60*1000*1000 + (time_array[2] - base_time_array[2])*60*1000*1000 + (time_array[3] - base_time_array[3])*1000*1000 + time_array[4] - base_time_array[4]
  print elapsed_time " " $2 " " $3 " " $5 " " $6 " " hash " " $7
}' ${TIME_DUMP} > ${METADATA}

rm -r ${TEMP}

echo "Operation Completed."
