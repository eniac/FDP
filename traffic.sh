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
  # containing the 13th-33rd byte of the packet, but excluding the 21st, 23rd and 24th bytes
  # Formate of each row in temp file is as follows:
  # time_stamp source target packet_identifier
  awk -v SUFFIX="${src_dest}" '{ 
    if(substr($1,1,2)!="0x") { 
      printf "%s %s ", $1, SUFFIX 
    }
    if(substr($1,1,7)=="0x0000:"){
      printf "%s%s", $8, $9
    }
    if(substr($1,1,7)=="0x0010:"){
      printf "%s%s00%s0000%s%s%s%s", $2, $3, substr($4,3,2), $6, $7, $8, $9
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

# Convert time stamps into elapsed time, final output will be saved into time_stream.txt
echo "Calculating elapsed time, and hash of packet identifier..."
# Take the first packet time stamp, parse it and set it's elapsed time to zero
time=$(head -n 1 ${TIME_DUMP})
readarray -d " " -t time_stream <<< ${time}
time_stream[0]=${time_stream[0]//./:}
time_stream[3]=${time_stream[3]//$'\n'/}
readarray -d : -t time_array1 <<< ${time_stream[0]}
elapsed_time=0
hash=$(echo -n ${time_stream[3]} | md5sum | cut -d' ' -f1)
echo "$elapsed_time ${time_stream[1]} ${time_stream[2]} ${hash}" >> ${TIME_STREAM}

# Iterate thrugh all the packets and find out elapsed time and hash of packet identifier 
while read time; do
  readarray -d " " -t time_stream <<< ${time}
  time_stream[0]=${time_stream[0]//./:}
  time_stream[3]=${time_stream[3]//$'\n'/}
  readarray -d : -t time_array2 <<< ${time_stream[0]}
  interval=$(( (10#${time_array2[0]} - 10#${time_array1[0]})*60*60*1000*1000 + (10#${time_array2[1]} - 10#${time_array1[1]})*60*1000*1000 + (10#${time_array2[2]} - 10#${time_array1[2]})*1000*1000 + 10#${time_array2[3]} - 10#${time_array1[3]} ))
  elapsed_time=$((${elapsed_time} + ${interval}))
  hash=$(echo -n ${time_stream[3]} | md5sum | cut -d' ' -f1)
  echo "$elapsed_time ${time_stream[1]} ${time_stream[2]} ${hash}" >> ${TIME_STREAM}
  time_array1=("${time_array2[@]}") 
done <<<$(tail -n +2 ${TIME_DUMP})

rm -r ${TEMP}

echo "Operation Completed."
