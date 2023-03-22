# Inventory Management

This web application provides the solution to the management of hardware assets within a company. The project is built upon Angular 14 (Presentation Layer), ASP.NET Core Web API (Domain Layer), and Entity Framework Core (Data Layer), following the characteristics of a single-page application (SPA) by using Angular router that aims to provide a seamless experience while navigating through each component of the app. This web app was developed by Minh Tran (tranthaiminh98@outlook.com).

# Main Functions

The basic UI of the web app enables you to:
  * Create, update a device, get an overview of the current stock.
  * Query list of users from active directory.
  * Manage the status of a specified device based on various actions (assigning, withdrawing, maintaining, decommissioning, etc.)
  * Quickly send emails to user for confirmation of the handover, receive notifications when confirmations (or rejections) happen and be reminded of expired confirmation requests.
  * Quick assign multiple devices to a single user, or withdraw all devices from a user (cart transfers).
  * Manage based on the amount for devices with no attached identifiable code (mouses, keyboards).
  * Import list of devices and other available data, such as user assignations, from Excel.
  * Generate device reports and user reports.
  * Manage which users can access the web app, login with Active Directory, config SMTP Server and more.

# Business Logics

This section describes the logics and assumptions implemented in this web app.

### 1. Statuses of a device

A typical device is only allowed to have a single status out of the list below:
 - In Stock
 - In Use
 - Decommissioning
 - Maintaining
 - Damaged
 - Decommissioned

### 2. Types of devices

Device types are further divided into 2 categories: Identified and Unidentified.

#### 2.1 Identified devices

An identified device has the following characteristics:

- Have a unique code to differentiate with others of the same type, these codes are referred as *Service Tags*.
- Is managed based on the device's Service Tag and its status.
- Examples usually include Laptop, Desktop.
- Must have its Service Tag and Device Type specified, with all of the other fields being optional: Device Name, Warranty Date, Device Model, PO Number.
- In case of damage/accident, can be transferred to the manufacturer for maintenance.
- Can retrieve the current user and the last 2 users.

#### 2.2 Unidentified devices
  
An unidentified device has the following characteristics:

- Does not have a unique code and other information of an identified device.
- Is managed based on the quantity.
- Cannot be maintained in case of damage/accident, which means it is broken immediately.
- Can only retrieve the current user.

### 3. Transfers

Transfer is the transition of a device or multiple devices from the current status to another status depending on the type of the transfer. There are multiple types of transfer. Transfers targeting a specified user only take place after the user has clicked on the confirmation link, which is sent along with the device information in an email.

#### 3.1. Boarding

- This transfer takes place when a device is added to the stock, either by using the "Add New" button of the UI or using the import feature - the "Import Device" button.

#### 3.2. Assignation

- An assignation will switch the status of a device to *In Use*. Only devices with *In Stock* status can implement this transfer. A user must be specified for this transfer.
- After interacting with the UI, an email confirmation will be sent to the user in question. The user needs to click the confirmation link for the transfer to execute.

#### 3.3 Reassignation

- A reassignation takes place when the user needs to replace their current device for a new one. The old device will be put back in stock. A reassignation is equivalent to a withdrawal of the current device from the user and an assignation of the new device to the user. A user must be specified for this transfer.
- After interacting with the UI, an email confirmation will be sent to the user in question. The user needs to click the confirmation link for the transfer to execute.

#### 3.4 Withdrawal
- A withdrawal will switch the status of a device to *In Stock*. Only devices with *In Use* status can implement this transfer. A user must be specified for this transfer.
- After interacting with the UI, an email confirmation will be sent to the user in question. The user needs to click the confirmation link for the transfer to execute.

#### 3.5 Maintainance
- This transfer is only applicable to identified devices. You can maintain *In stock* or *In use* devices.

***Note***: If a device is damaged by a user and needs maintaining, you must assign another *In stock* device to this user. After you selected a new device and a user, a confirmation dialog will appear, asking you to whether move the old device back to stock (reassignation) to stock or to maintain it (maintainance).

#### 3.6 Restock
- You can restock a device with *Maintaining* status. Here you can switch its status to *In stock*, if the device has been repaired successfully, or to *Damaged*, if it is impossible to repair the device.

#### 3.7 Decommission
- A device needs decommissioning when it is no longer usable. Only devices with *In Stock* or *Damaged* statuses can perform this transfer. A decommissioned device cannot perform any transfer.

# UI images


![image](https://user-images.githubusercontent.com/101991304/220037997-95dfd9f1-cc73-4c5a-be9e-512f2dfcd592.png)


![image](https://user-images.githubusercontent.com/101991304/220285437-1475e053-9709-4151-8fdb-d68d5f6a7bf9.png)


![image](https://user-images.githubusercontent.com/101991304/220286018-57d87c33-a879-48c3-8287-5deb4e3b56c4.png)

The actions display based on the status of the selected device
![image](https://user-images.githubusercontent.com/101991304/220286107-0f571e88-4369-4f79-b281-ee26f9b13579.png)

![image](https://user-images.githubusercontent.com/101991304/220286178-32bdb2a4-5aa6-44af-a2e5-1df673944ac9.png)
![image](https://user-images.githubusercontent.com/101991304/220286211-95c9a091-9ee1-46e6-adb2-8e5bb36c0d95.png)


![image](https://user-images.githubusercontent.com/101991304/222340914-a1295dc3-cc2b-41ef-8362-82beb3fb7870.png)
![image](https://user-images.githubusercontent.com/101991304/222340705-e9504514-2c9a-487a-b8aa-e39e8d77557c.png)
![image](https://user-images.githubusercontent.com/101991304/222340995-a89938f2-e2d6-4e6a-afe8-7ae6e132be9a.png)


![image](https://user-images.githubusercontent.com/101991304/223093552-73ad5558-b1f6-4e46-aa6b-5759f6660ae5.png)

# Contact

Please send an email to tranthaiminh98@outlook.com in case you have any queries for this app.

  
