#!/usr/bin/env bash

# Access pcap directory
if [ -z "$1" ]
then
  echo "Usage: ./traffic.sh <pacp directory path>"
  exit
fi

PCAP_DIR=$1
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
fi

# Removing existing output folder and create new one
OUTPUT=output
if [ -d ${OUTPUT} ] 
then
	rm -r ${OUTPUT}
fi
mkdir ${OUTPUT}

# Creating empty time dump file
TIME_DUMP=${OUTPUT}/time_dump_log.txt
echo "Using TIME_DUMP = ${TIME_DUMP}"
touch ${TIME_DUMP}
> ${TIME_DUMP}

# Creating empty time dump file
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
  # Take the tcp dump to temp file
  tcpdump -x -r ${pcap_file} > ${TEMP}
  # Store the formated output to the temp file
  # fourth column is a unique identifier of the packet 
  # containing the 22th-33rd byte of the packet, but excluding the 23rd and 24th bytes
  # Formate of each row in temp file is as follows:
  # time_stamp source target packet_identifier
  awk -v SUFFIX="${src_dest}" '{ 
    if(substr($1,1,2)!="0x") { 
      printf "%s %s ", $1, SUFFIX 
    }
    if(substr($1,1,7)=="0x0010:"){
      printf "%s0000%s%s%s%s", substr($4,3,2), $6, $7, $8, $9
    }
    if(substr($1,1,7)=="0x0020:"){
      printf "%s\n", substr($2,1,2)
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
  cmd="echo -n " $4 " | md5sum|cut -d\" \" -f1"; cmd|getline hash;
  close(cmd)
  split(BASE, base_time_array, /[:.]/)
  split($1, time_array, /[:.]/) 
  elapsed_time=(time_array[1] - base_time_array[1])*60*60*1000*1000 + (time_array[2] - base_time_array[2])*60*1000*1000 + (time_array[3] - base_time_array[3])*1000*1000 + time_array[4] - base_time_array[4]
  print elapsed_time " " $2 " " $3 " " hash
}' ${TIME_DUMP} > ${TIME_STREAM}

rm -r ${TEMP}

echo "Operation Completed."
