# Servitore Enterprise Deployment Guide

This guide explains how to set up, configure, and deploy the **Servitore** multi-user application in a corporate local network environment.

---

## 1. Database Setup (Central SQL Server)

Servitore uses a single centralized Microsoft SQL Server database. Every installation of Servitore (either local client-server or central API-server) connects to this database.

### Prerequisites
* Microsoft SQL Server (2016 or newer, Express edition or higher).
* SQL Server Management Studio (SSMS).
* Mixed Mode Authentication enabled in SQL Server (required if connecting via username/password).

### Steps to Initialize the Database
1. Open SSMS and connect to your SQL Server instance.
2. Open a new query window and run the following script to create the database:
   ```sql
   CREATE DATABASE ServitoreDB;
   GO
   ```
3. Create a login and user for the Servitore application (recommended for SQL Authentication):
   ```sql
   USE master;
   GO
   CREATE LOGIN servitore_user WITH PASSWORD = 'YourStrongPassword123!';
   GO
   USE ServitoreDB;
   GO
   CREATE USER servitore_user FOR LOGIN servitore_user;
   GO
   ALTER ROLE db_owner ADD MEMBER servitore_user;
   GO
   ```

---

## 2. API Hosting & Configuration

The WPF desktop application interacts with the database via the centralized Web API (`Servitore.API`). For a multi-user ERP setup, the API must be hosted centrally so that all client computers can connect to it.

### Step 2.1: Host the API
You can run the API on a central network server (e.g. Windows Server) in one of two ways:
1. **Host on IIS (Internet Information Services)** (Recommended for production):
   - Install IIS with the .NET Core Hosting Bundle.
   - Publish the API: `dotnet publish Servitore.API -c Release -o C:\inetpub\servitore-api`
   - Point an IIS Website to `C:\inetpub\servitore-api` and configure port `5000` or `5001`.
2. **Run as a Windows Service**:
   - Register the published API executable as a Windows Service using `sc.exe`.

### Step 2.2: Configure Database Connection
In the hosted API directory, create or edit `databaseSettings.json`:
```json
{
  "DatabaseSettings": {
    "Server": "YOUR_SQL_SERVER_INSTANCE_NAME",
    "Database": "ServitoreDB",
    "Username": "servitore_user",
    "Password": "YourStrongPassword123!"
  }
}
```
* **Integrated Windows Authentication**: If you prefer Windows Authentication, leave `Username` and `Password` blank (`""`), and ensure the Windows identity running the API process (e.g., AppPool Identity or Active Directory service account) has `db_owner` permissions on the database.
* **Auto-Migrations**: Upon startup, the API will automatically verify the database schema, apply any pending Entity Framework migrations, and seed the default roles/administrator if they don't exist.

---

## 3. Client Installation and Deployment

Once the central API is running, the desktop clients can be deployed on the administrative PCs.

### Step 3.1: Configure Client Endpoint
Before packaging, or inside the client's output folder, locate `clientSettings.json` (or `clientSettings.Production.json` during build):
```json
{
  "ApiBaseUrl": "http://your-server-ip-or-hostname:5000",
  "IdleTimeoutMinutes": 10
}
```
Point `ApiBaseUrl` to your hosted central API server's URL.

### Step 3.2: Build and Publish the Client
To compile a self-contained release build with the .NET runtime included (ensuring it runs on any Windows PC without additional dependencies):
```powershell
dotnet publish Servitore.Desktop\Servitore.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish\desktop
```

### Step 3.3: Install on Client PCs
1. Copy the `publish\desktop` folder and the `install.ps1` script to the target client computer (e.g., via a network share or USB drive).
2. Right-click `install.ps1` and select **Run with PowerShell** (or run it from an elevated PowerShell command prompt).
3. The installer will copy all binary files to `C:\Program Files\Servitore`, configure shortcuts, and register them on the desktop and start menu.

---

## 4. How Live Data Sync Works

Servitore uses a dual-synchronization layer to keep data live across all user machines:
1. **Central Database Store**: Every change is saved instantly to the single SQL Server database.
2. **Real-time SignalR Broadcasts**: When an admin performs a CRUD action (Add, Edit, Delete), the desktop app notifies the API, which broadcasts a lightweight SignalR event (e.g., `DataChanged`) to all other active desktop instances. 
3. **Automatic background refresh**: Upon receiving the SignalR broadcast, the other clients immediately reload their view list silently in the background, keeping the display current.
