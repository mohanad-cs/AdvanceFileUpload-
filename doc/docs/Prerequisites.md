# **Prerequisites**

Before you begin the installation and setup process, ensure your development and deployment environments meet the following requirements. A proper setup is crucial for both the stability of the API and a smooth development experience.

## **Software Requirements**

* **.NET 8 SDK**: The API is built entirely on the .NET 8 platform. The SDK is essential as it provides the necessary runtime, libraries, and command-line tools to build, test, and run the project.  
* **IDE**: A .NET-compatible Integrated Development Environment (IDE) is highly recommended for an efficient workflow. An IDE provides critical features like code completion, debugging tools, and integrated project management. Options include:  
  * Visual Studio 2022 (Recommended)  
  * JetBrains Rider  
  * Visual Studio Code with the C\# Dev Kit extension  
* **Database**: A running instance of **SQL Server**. The system relies on a database to persist the state of all upload sessions. This is the backbone of the resumable upload functionality, as it tracks metadata for each session, including which chunks have been successfully received. This stateful approach ensures that uploads can survive network interruptions and be resumed later.  
* **Message Broker**: A running instance of **RabbitMQ**. This is only required if you enable the optional integration event publishing feature (EnableIntegrationEventPublishing in appsettings.json). When enabled, the API can publish messages to RabbitMQ upon events like upload completion, allowing other microservices (e.g., a video transcoder or a security scanner) to react to new files asynchronously.  
* **Git**: A version control client like Git is needed to clone the source code repository from GitHub.

## **System Requirements**

* **Operating System**:  
  * **Development**: The project can be developed on Windows, macOS, or Linux, thanks to the cross-platform nature of .NET.  
  * **Deployment**: The project is pre-configured to run as a **Windows Service**. This is the recommended deployment model for production on a Windows Server, as it allows the API to start automatically on boot and run continuously in the background without requiring an active user session. Alternatively, the application can be containerized using Docker for deployment on Linux or in a cloud-native environment.  
* **Hardware**:  
  * **Minimum**: 2 CPU cores, 4 GB RAM.  
  * Recommended: 4+ CPU cores, 8+ GB RAM. 
>[!NOTE] 
> Handling concurrent file uploads is resource-intensive. The recommended hardware ensures the server can manage the significant network and disk I/O from receiving and storing multiple chunks simultaneously, as well as the CPU load from file merging and optional compression, ensuring responsive performance under production load.  

* **Network**: A stable network connection is paramount. The server hosting the API must have firewall rules configured to allow inbound traffic on the necessary ports.  
  * **HTTP/HTTPS (e.g., 5124, 443\)**: For the main API communication.  
  * RabbitMQ (e.g., 5672): For the message broker, if used.  

