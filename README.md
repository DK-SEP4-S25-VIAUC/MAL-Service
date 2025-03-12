# MAL-Service
The Machine Learning Microservice module for the SEP4 Smart Greenhouse Project

## Running the Development Environment:
After initially installing / setting up the environment, as described further down, you will be able to simply launch the development environment from the 'Remote Development' -> 'Dev Containers' Pane in Rider:

![SecondTime_Run](https://github.com/user-attachments/assets/25ab95de-2358-4e08-b0cd-c814dcc9a2bf)




## Setting up the Development Environment:

### Import Git username/email automatically during container build:
If these options are not set, you just need to manually enter your git user_name and git user_email manually in the container before being allowed to commit anything to the shared GitHub repo.

1. Launch Windows Powershell as an administrator
2. Check if you already have a powershell profile set, by typing: ```Test-Path $PROFILE```
3. If the step above returns False: Type ```New-Item -Path $PROFILE -ItemType File -Force``` to create a powershell profile.
4. Type ```Notepad $PROFILE``` to open the profile with windows notepad.
5. Insert these lines, and save (CTRL + S):
   ```
   $env:GIT_USER_NAME = git config --global user.name
   $env:GIT_USER_EMAIL = git config --global user.email
   ```
7. Type ```. $PROFILE``` to reload the profile.
8. If step 6 causes a 'cannot be loaded because running scripts is disabled on this system.... etc' error, then follow the steps below. Else jump to step 8.
    1. Type ```Get-ExecutionPolicy``` and check that it is set as 'RemoteSigned'. If not, we must set it.
    2. Type ```Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned```
    3. Confirm the changes by typing 'y'
    4. Run ```Get-ExecutionPolicy``` again and check that the execution policy is now properly set to 'RemoteSigned'
9. You are now set. Continue with the import and setup described below.



### First time install / Setting up the development environment directly from GitHub
IMPORTANT: Ensure that Docker Desktop is running in the background - and that Docker Desktop along with other sub-requirements such as WSL are properly installed!

1. Launch Rider on your PC and close any already open projects.


2. Select 'Dev Containers' and then click on 'New Dev Container'

![Step2](https://github.com/user-attachments/assets/7b18ef2e-e1df-4e8a-81dd-903f8dc138e1)



3. Select the options displayed on the picture below, inserting the proper repository link into the Git Repository field. Select the branch you wish to initially check out.

![Step3](https://github.com/user-attachments/assets/537aab6b-fada-4f88-8c73-2725df41b531)



4. Wait for Rider to download the repository, dev enviroments and build the local container.

![Step4](https://github.com/user-attachments/assets/d49deee8-43cf-4ea5-9876-8e985b96b69d)



5. The Rider Dev Environment will automatically launch. You now have the latest Rider version running in your dev environment with the team specified libraries/packages and configurations already prepared.

![Step5](https://github.com/user-attachments/assets/aa5e7c87-a582-4de8-85b5-b18faabe2417)

   
   
   
