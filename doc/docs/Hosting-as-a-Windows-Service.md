# **Hosting as a Windows Service**

The Advance Chunk File Upload API is pre-configured to be deployed and run as a background Windows Service, which is the recommended approach for production environments on Windows Server. This ensures the API starts automatically with the server and runs continuously without a user session.

## **1\. Publishing the Application**

First, you need to publish the application as a self-contained executable for Windows.

1. Open a terminal or command prompt in the root directory of the AdvanceFileUpload.API project.  
2. Run the following dotnet publish command:  
   dotnet publish \-c Release \-r win-x64 \--self-contained true

   * \-c Release: Builds the project in Release mode for production optimization.  
   * \-r win-x64: Specifies the target runtime as Windows 64-bit.  
   * \--self-contained true: Packages the .NET runtime with the application, so it doesn't need to be installed on the server.  
3. The output will be located in the bin\\Release\\net8.0\\win-x64\\publish directory. Copy the entire contents of this publish folder to your production server (e.g., C:\\apps\\FileUploadAPI).

## **2\. Creating the Windows Service**

You will use the built-in sc.exe (Service Control) command-line tool on the production server to create the service.

1. Open Command Prompt or PowerShell **as an Administrator**.  
2. Run the sc create command. You must provide a service name and the full path to the published executable.  
   sc.exe create "AdvanceFileUploadAPI" binPath="C:\\apps\\FileUploadAPI\\AdvanceFileUpload.API.exe"

   * Replace "AdvanceFileUploadAPI" with your desired service name.  
   * Replace the binPath with the correct path to the executable on your server.  
   * **Important**: Note the space after binPath=. It is required by sc.exe.  
3. If the command is successful, you will see \[SC\] CreateService SUCCESS.

## **3\. Configuring and Starting the Service**

Once the service is created, you can configure its properties and start it.

1. **Set Startup Type (Optional but Recommended)**: Configure the service to start automatically when the server boots.  
   sc.exe config "AdvanceFileUploadAPI" start=auto

2. **Set a Description (Optional)**: Add a helpful description that will appear in the Services management console.  
   sc.exe description "AdvanceFileUploadAPI" "Handles large, resumable file uploads for company applications."

3. **Start the Service**:  
   sc.exe start "AdvanceFileUploadAPI"

You can also manage the service (start, stop, restart) through the graphical Services management console (services.msc).

## **4\. Logging and Monitoring**

For production use, it's crucial to monitor the service's health and logs.

* **Windows Event Log**: The application is configured with **Serilog** to write logs directly to the Windows Event Log.  
  * Open the **Event Viewer** (eventvwr.msc).  
  * Navigate to **Windows Logs \> Application**.  
  * Look for events with the Source set to AdvanceFileUploadAPI. This is where all application logs, including informational messages, warnings, and errors, will be recorded.  
* **Health Check Endpoint**: The API exposes a health check endpoint at /health. You can use monitoring tools to periodically ping this endpoint to ensure the service is running and responsive.