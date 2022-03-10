# Unity 120hz Eye Tracking Example
This code provides an example of using the Vive Pro Eye tracker for VR eye tracking at full frame rate through SRanipal. Includes code for recording data and using data from eye tracker in Unity along with some simple experiment examples. 

Code is shared here as a service to academic research communities and hopefully it helps some of you researchers who have had to become developers in your spare time. If this code is directly used or inspires code for your own academic project please cite for any publications stemming from the project: 

Lamb, M., Brundin, M., Pérez Luque, E., Billing, E. (2022) Eye-tracking beyond peripersonal space in virtual reality: Validation and Best practices. Frontiers in Virtual Reality

If this code is used in a commercial product consider supporting us and/or our research materially or financially. If it saves you time and money maybe pass it along?

## Table of Contents
* [General Info](#general-information)
* [Technologies Used](#technologies-used)
* [Features](#features)
* [Setup](#setup)
* [Usage](#usage)
* [Project Status](#project-status)
* [Acknowledgements](#acknowledgements)
* [Contact](#contact)
* [License](#license)

## General Information
- This project was developed for data collection in our lab and to demonstrate how to think about eye-tracking using consumer and entertainment focused hardware/software systems for human subjects data collection. It will almost certainly be obsolete before anyone uses it, but hopefully the some of the basics and approaches will make future resaerch and development easier for researchers. My own process and frustations 

## Technologies Used
- Unity (original data collected on 2018.4, tested on 2019.4 and seemed to work) 
- HTC Vive Pro Eye
- SRanipal v. 1.3.3 (download from HTC) 
- OpenVR (included)
- SteamVR - It is hard to choose here, autoupdates are sneaky. So far the current version works (as of 10/3/22). One version of SteamVR broke the callback to SRanipal and resulted in intermitant dropped data frames but that seems fixed now. Keep an eye on this. 
- Only tested in Windows 10

## Features
List the ready features here:
- 120 Hz data collection of both eye and head data (direct from OpenVR)
- Examples of simple data collection setups
- VOR task
- Helper functions for calculating key data variables
- Write relevant data to .csv file


## Setup
SRanipal SDK is required to run. Download from (https://developer.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/). 
- Install relevant files in the assets folder (Scripts file is absolutely necessary SranipalEyeFramework prefab is also used)
- Eye tracking must be enabled from inside the headset for first use
- SRanipalEyeFramework needs some code added to it from AdditionsToSRFramework.cs. Uncomment the code in AdditionsToSRFramework.cs and use it to replace or add code from SRanipalEyeFramework.cs. This adds an additional state messsage to the framework and delays start of the framework to solve an issue with load times. You can test without these additions, it has been a while since I have tried it.

HTC Vive Pro Eye can be setup like a regular Vive/Vive Pro and run through SteamVR. 

The Unity project should be setup to run VR/XR.
- NOTE: Incompatable with XR manager in 2019.4 and later. Works in 2019.4 using old VR system. The issue with the new XR manager is in the call to OpenVR to get the HMD position/orientation at 120hz. If you don't need these values then this can be bypassed and should work if you pass the camera values into the recieve thread with global vars. We would be interested in any solutions to make this work with XR manager. 

Make sure relevant scenes are added to the build settings (in our tests all setting seem to move over just fine). 


## Usage
If all goes well, with a HTC Vive Pro Eye connected to the computer, then use the Menu_PreSelectedOrder scene to initialize. 

Make sure that SRanipalEyeFramework is set to eyedata v2

Calibration should be run from the menu to make Start button interactable. Set a participant number and select start after calibration. After calibration rough statistic should be displayed in real-time. 

Examples should run on start. 

VOR task (HMDLag scene) can be run from Menu_SingleSelect scene. 

Moving to your own project:
The key files are **ViveEyeDevice.cs** and **ViveEyeController.cs** found in Assets\Scripts_Vive\EyeTracking\. 

*ViveEyeDevice.cs* handles the thread for pulling data from eye tracker at 120hz. 
- ViveEyeDevice uses an Action that can be subscribed to in order to effeciently share data with other classes
- EyeData is parsed to a dictionary here. This isn't necessary as the eyedata parameter can be passsed around. It is partly done for convinience in the data writting phase. Switching to eyedata will require some reworking of code in the rest of the project.
- This is not a MonoBehaviour derived class. It cannot be added to a gameobject in the editor, see how it is initialized in ViveEyeController.cs.*- Note this runs a seperate thread from Unity. It is a good idea to call StopDevice() on shutdown from a MonoBehaviour to ensure that the thread is properly shutdown. 

*ViveEyeController.cs* is where all of the eye-tracking calcluations are handled and data is transferred between the 120hz thread and the main Unity thread (90hz). This class is designed to be extended for one's own purposes.
- Recieve is invoked every time new eyedata is recieved by ViveEyeDevice. This triggers a cascade of calculations starting with checking the current state of the HMD (and controllers) from OpenVR. 
- AdditionalVars can be calculated for ones own purposes. 
- Data from Recieve is passed to the main Unity thread via a ConcurrentQueue. Update checks and clears the queue so that only the latest data from the eye based calculations are used for the Unity interactions. 
- I have used publically accessable varibles so that other scripts can refernce the instance of ViveEyeController in the scene and read those values. See example of this in InitializerGazeBehavior.cs

*DataRecorder.cs* handles creating csv files and recording data
- Called from the end of Recieve in ViveEyeController. 
- Need to setup a new file and start recording.
- recording can happen with each new eyedata frame (safer but may cause slow downs, though I haven't experienced issues) or when recording is stopped (could cause a bit of hang for long record sessions and risks losing data on unexpected shutdown).
- In example scenes recording is handled in the the experiment logic of the relevant TaskController derived class
- Recorded files found in \users\<username>\AppData\LocalLow\<CompanyName>\<UnityProjectName>\EyeRecordingData\ (company name may  be default company if you do not set it or iLab_Skovde, you may also set company name yourself in Unity settings)


*TaskController.cs* is used to run the trial logic and load scenes. There are versions of this for each of the example scenes. Using coroutines is a little impicise for timing, so only use that when millisecond precision isn't needed. 


Various other files handle stimulus placement and sizing in eye angle units. 

## Project Status
Project is: _Limited Development_

Relevant updates made in the lab will be pushed here or on my Github site. We will not be actively monitoring issues or pull requests. We are not professional software developers, just researchers. We hope something here can help a few people out. We're interested in how it is used or interesting ideas, but have all the normal pressures and expectations of acadademic life and none of the pay of developers.

## Contact
Primary development done by Maurice Lamb (mauricelamb.com), I'm currently a Senior Lecturer at the University of Skövde. My resaerch is focused on HRI, artifical agents, and human-human interaction. 

## License
This project is open source and available under the BSD 2-Clause License

Copyright (c) 2022, Maurice Lamb
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

You don't have to include all sections - just the one's relevant to your project