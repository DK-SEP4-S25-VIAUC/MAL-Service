# MAL-Service
The Machine Learning Microservice module for the SEP4 Smart Greenhouse Project

<br><br><br>

## Standard Libraries included in the Dev environment:

### Main Project: MAL-Api-Service initialized with:
  
| Package                  | Description                                                                                                                                                                                                        |
|--------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AspNetCore (Swashbuckle) | Generates an Swagger/OpenAPI documenation and UI for the ASP.NET Core API Endpoints                                                                                                                                |
| AspNetCore.Hosting       | Provides core hosting infrastructure for ASP.Net Core apps. Useful when deploying to Azure AKS (or other), since this package provides a self-contained hosting model which is required by several Azure services. |
| EntityFrameworkCore      | An ORM for database access, enabling LINQ queries and migrations in .NET. Can be used with Azure SQL cloud hosted databases.                                                                                       |
| Newtonsoft.JSON          | A high-performance JSON serializer/deserializer for complex object handling in .NET.                                                                                                                               |
| RestSharp                | Simplifies HTTP requests (e.g., REST API calls) with a fluent client interface.                                                                                                                                    |
  
<br>

### Testing Project: MAL-Api-Service.Tests initialized with:

| Package                | Description                                                                                                            |
|------------------------|------------------------------------------------------------------------------------------------------------------------|
| xunit                  | A modern unit testing framework for writing and running tests in .NET.                                                 |
| .Net.Test.Sdk          | Provides the core testing infrastructure for .NET test projects (required by test runners).                            |
| Moq                    | A mocking library for creating fake objects to isolate and test dependencies.                                          |
| FluentAssertions       | Enhances test readability with expressive assertion syntax (e.g., result.Should().Be(5)).                              |
| Coverlet.Collector     | Collects code coverage data during dotnet test runs, allowing easy evaluation of how much of the code base was tested. |
| AspNetCore.Mvc.Testing | Facilitates integration testing of ASP.NET Core MVC apps with an in-memory test server.                                |
  
<br> 

### Special dependencies available inside the ubuntu container: These are very much up for grabs. If they end out not needed, we should delete them.

Initially these are included so that the devcontainer can interact with python based code/projects and vice-versa.

| Package        | Description                                                                          |
|----------------|--------------------------------------------------------------------------------------|
| pythonnet      | Enables calling .NET code from Python (interop between Python and C#).               |
| azureml-core   | Core SDK for Azure Machine Learning, used for managing ML workflows and experiments. |
| azureml-mlflow | Integrates MLflow (tracking ML experiments) with Azure Machine Learning.             |

<br><br><br>

## Running the Development Environment:
After initially installing / setting up the environment, as described further down, you will be able to simply launch the development environment from the 'Remote Development' -> 'Dev Containers' Pane in Rider:

![SecondTime_Run_red](https://github.com/user-attachments/assets/d7bf3516-e389-45ee-a33e-4fec3d739a59)  

  <br><br><br>
  
  
  
## Setting up the Development Environment:

### OPTIONAL: Import Git username/email automatically during container build:  

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

<br><br>

### SEMI-REQUIRED: Setting up Self-signed certificates -> Required for testing/hosting HTTPS based endpoints/server  

1. Clear any already existing self-signed certificates for localhost (i.e. maybe an SSL from SEP3).
   
    a. Press CTRL + R and then type ```certmgr.msv``` to open the certificate manager i windows.
   
    b. Navigate to ```Trusted Root Carficiation Authorities / Rodnøglecentre, der er tillid til``` -> ```Certificates / Certifikater ```
   
    c. Delete <b>ALL</b> certificates issued to localhost (there may be multiple!). You can easily search for them, by just typing "L" with the keyboard.
   
4. Download the self-signed certificates form here ([dev_certs.zip](https://github.com/user-attachments/files/19235235/dev_certs.zip)). They are also included in the C# solution source - but must be run on your local windows machine (not inside the docker container/dev-environment), so you'll anyhow need to download these to your windows machine.
5. Unpack contents
6. Right-click `setup-cert.bat` and select "Run as administrator" (or if you're using Linux/Unix, run 'setup-cert.sh' as administrator)
7. Follow the prompts to import the certificate.
<br>
If you've already installed the dev environment you can now:

  1. Start the container via Rider Remote Development.
     
  3. Test endpoints at `https://localhost:8081`.

Otherwise, follow the "first time install" instructions below.  


<br><br>
### REQUIRED: First time install / Setting up the development environment directly from GitHub  

IMPORTANT: 
- Ensure that Docker Desktop is running in the background - and that Docker Desktop along with other sub-requirements such as WSL are properly installed!
- Ensure that you have installed the localhost development SSL certificates (Self-signed) for development - otherwise testing https during development will be "impossible".

<br>

1. Launch Rider on your PC and close any already open projects.

<br><br>

2. Select 'Dev Containers' and then click on 'New Dev Container'

![Step2_red](https://github.com/user-attachments/assets/c3072c34-d306-4aed-aacc-0baecc65896f)

<br><br>


3. Select the options displayed on the picture below, inserting the proper repository link into the Git Repository field. Select the branch you wish to initially check out.

IMPORTANT: Ensure to manually select the devcontainer.json from the dotnet environment in the repo! Otherwise it won't work.

![step3_red](https://github.com/user-attachments/assets/2e076afb-c142-41f7-9737-90573dd81c27)

<br><br>


4. Wait for Rider to download the repository, dev enviroments and build the local container.

![Step4_red](https://github.com/user-attachments/assets/8d2ce0f6-766d-484f-94d2-f9779ae5d1e0)

<br><br>


5. The Rider Dev Environment will automatically launch. You now have the latest Rider version running in your dev environment with the team specified libraries/packages and configurations already prepared.

![Step5_red](https://github.com/user-attachments/assets/6968b941-e005-4e89-bbd1-3af59abdf373)

<br><br>
   
   
   
