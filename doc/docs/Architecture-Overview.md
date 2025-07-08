# **Architecture Overview**

The Advance Chunk File Upload API is built on a set of modern architectural patterns designed to ensure scalability, maintainability, and a clear separation of concerns. This document describes the high-level architecture of the system.

![](../images/System%20Arc.png)

## **Core Architectural Patterns**

The system's design is guided by the following principles:

* **Client-Server Architecture**: The system is split into two main parts: a server-side API that handles all the core logic, and a client-side SDK that simplifies integration for developers.  
* **Layered Architecture**: The server application is organized into four distinct layers, each with a specific responsibility. This enforces a strict separation of concerns.  
* **Domain-Driven Design (DDD)**: The core business logic is modeled within a rich Domain layer, which is completely independent of infrastructure concerns like databases or APIs. The FileUploadSession is the central "Aggregate Root" that enforces all business rules for an upload.  
* **Event-Driven Architecture (EDA)**: The system uses events to communicate between different parts of the application in a decoupled manner. This is used for both internal workflows (e.g., notifying the system to merge chunks) and external integrations via RabbitMQ.  
* **Mediator Pattern**: In-process communication between components is managed by the Mediator pattern (using the MediatR library), which reduces direct dependencies and keeps the application logic clean.

## **System Components**

### **Client Tier**

* **Client SDK (AdvanceFileUpload.Client)**: A .NET library (distributed as a NuGet package) that abstracts the entire upload process. It handles session creation, file chunking, parallel chunk uploads, and automatic retries for failed requests using the Polly library. Developers interact with a simple IFileUploadService interface.

### **Server Tier**

The server is composed of several logical layers, typically organized as separate projects within the solution.

1. **API Layer (AdvanceFileUpload.API)**:  
   * This is the entry point for all external communication.  
   * It exposes RESTful HTTP endpoints for actions like creating sessions, uploading chunks, and canceling uploads.  
   * It handles cross-cutting concerns like authentication (API Key validation), rate limiting, and global exception handling.  
   * It hosts a SignalR hub for real-time progress notifications.  
2. **Application Layer (AdvanceFileUpload.Application)**:  
   * This layer orchestrates the business workflows or "use cases."  
   * It contains services like UploadManager that receive requests from the API layer.  
   * It does not contain business logic itself but directs the Domain layer entities to perform actions and coordinates with the Data layer to persist results.  
   * It contains handlers for domain events.  
3. **Domain Layer (AdvanceFileUpload.Domain)**:  
   * The heart of the system. It contains all the business logic, rules, and state.  
   * Key entities include FileUploadSession and ChunkFile.  
   * It is completely isolated from infrastructure. It has no dependencies on databases, file systems, or network protocols.  
   * It raises domain events (e.g., ChunkUploadedEvent, FileUploadSessionCompletedEvent) to signal state changes.  
4. **Data Layer (AdvanceFileUpload.Data)**:  
   * This layer is responsible for all data persistence.  
   * It implements the IRepository\<T\> interface defined in the Domain layer.  
   * It uses Entity Framework Core to communicate with the SQL Server database, mapping domain entities to database tables.  
   * This layer isolates the rest of the application from the specific database technology being used.