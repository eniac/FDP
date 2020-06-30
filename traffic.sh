#!/usr/bin/env bash

# Access pcap directory
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
echo "Using TIME_DUMP = ${TEMP}"
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
  tcpdump -r ${pcap_file} > ${TEMP}
  # Store the formated output to the temp file
  awk -v SUFFIX="${src_dest}" '{ 
    if(substr($1,1,2)!="0x") { 
      print $1 " " SUFFIX 
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

echo "Finding out elapsed time..."
# Convert time stamps into elapsed time, final output will be saved into 
# time_stream=""
# Take the first packet time stamp, parse it and set it's elapsed time to zero
time=$(head -n 1 ${TIME_DUMP})
readarray -d " " -t time_stream <<< ${time}
time_stream[0]=${time_stream[0]//./:}
time_stream[2]=${time_stream[2]//$'\n'/}
readarray -d : -t time_array1 <<< ${time_stream[0]}
elapsed_time=0
echo "$elapsed_time ${time_stream[1]} ${time_stream[2]}" >> ${TIME_STREAM}

# Iterate thrugh all the packets and find out elapsed time
while read time; do
  # time_stream=""
  readarray -d " " -t time_stream <<< ${time}
  time_stream[0]=${time_stream[0]//./:}
  time_stream[2]=${time_stream[2]//$'\n'/}
  readarray -d : -t time_array2 <<< ${time_stream[0]}
  interval=$(( (10#${time_array2[0]} - 10#${time_array1[0]})*60*60*1000*1000 + (10#${time_array2[1]} - 10#${time_array1[1]})*60*1000*1000 + (10#${time_array2[2]} - 10#${time_array1[2]})*1000*1000 + 10#${time_array2[3]} - 10#${time_array1[3]} ))
  elapsed_time=$((${elapsed_time} + ${interval}))
  echo "$elapsed_time ${time_stream[1]} ${time_stream[2]}" >> ${TIME_STREAM}
  time_array1=("${time_array2[@]}") 
done <<<$(tail -n +2 ${TIME_DUMP})

rm -r ${TEMP}

echo "Operation Completed."
