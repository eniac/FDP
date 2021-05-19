# Flightplan Demo Platform (FDP)
## Introduction
FDP is a portable demo/teaching platform for computer networking. It builds on existing tooling to enable end-to-end experimentation and zero-effort in-browser interactive visualization. It includes an example for [P4](p4.org)-based [SDN](https://en.wikipedia.org/wiki/Software-defined_networking) and research demonstration.
More information can be found on the [Flightplan website](https://flightplan.cis.upenn.edu/).

## Usage Examples
See an early [demo video](https://drive.google.com/file/d/149YrRqJxQ6aNmO6FqlRTm5p4N_QvQ-U6/view?usp=sharing):
<br /><a href="https://drive.google.com/file/d/149YrRqJxQ6aNmO6FqlRTm5p4N_QvQ-U6/view" target="_blank"><img src="https://www.seas.upenn.edu/~nsultana/fdp.png" alt="FDP video" height="200"/></a><br />
This was presented at [N2Women'20](http://conferences.sigcomm.org/sigcomm/2020/n2women.html) where it was selected for the runner-up award!

Check out the [demo](https://flightplan.cis.upenn.edu/sigcse21/) we presented at [SIGCSE'21](https://sigcse2021.sigcse.org/schedule/demos/).
Our [abstract](https://www.seas.upenn.edu/~nsultana/files/SIGCSE-abstract-FDP.pdf) sets the scene for the demo.


## Usage Instructions
1. Instructions to create a FDP-based demo is given in [Tutorial.pdf](Tutorial.pdf) and briefly mentioned below.
2. Instructions to work with source code is given in [TutorialUnity.pdf](TutorialUnity.pdf).

### Backend System
1. Download the `FDP_release` and `traffic.sh` from github and place them on the web server on which you are are going to host the FDP.
2. Download the `topology.yml` and `config.yml` from github containing template for the topology and configuration files respectively.
3. Create the experiment and generate the `pcap` files for the each node along the path of a packet from source host node to destination host node.
4. Create graph log files and place them in a directory, if required to show run time graph in the animation.
5. Place the images in a directory, if required to show them in the animation.
6. Update the `topology.yml` file based upon the experiment.
7. Update the `config.yml` file to customize the animation. If you are using graphs or images for the animation please provide this information too in the `config.yml` file.

##### _Creating The Experiment_
Execute the following command to generate the data readable to FDP:
```sh
./traffic -create <Streaming Asset Directory> <Name of Experiment> <Topology File> <pcap directory> <Configuration File> <Graph Directory> <Image Directory>
```
Where,
| Parameter | Description | Value Type |
| ------ | ------ | ------ |
| Streaming Asset Directory | Path of the directory `FDP_release/StreamingAssets` | String |
| Name of Experiment | Name of Experiment | `_` Saparated string  |
| Topology File | Path of `topology.yml` file | String |
| pcap directory | Path of the directory where `pcap` files are | String |
| Configuration File | Path of the `config.yml` file | String |
| Graph Directory | `Graph` directory path  | String/None |
| Image Directory | Path of the directory | String/None |

##### _Removing The Experiment_
Execute the following command to remove the existing experiment from the FDP experiment list:
```sh
./traffic -remove <Streaming Asset Directory> <Name of Experiment>
```
Where,
| Parameter | Description | Value Type |
| ------ | ------ | ------ |
| Streaming Asset Directory | Path of the directory `FDP_release/StreamingAssets` | String |
| Name of Experiment | Name of Experiment | `_` Saparated string  |

### Frontend System
1. Open the web browser (`Chrome` / `Firefox` / `Safari` / `MS Edge`).
2. Go to the FDP hosted website.
3. Choose the experiment from the drop down menu.
4. _There you have a rich interactive 3D network._

## License
[Apache 2.0](LICENSE)

## Author
[Heena Nagda](www.linkedin.com/in/nagdaheena)
