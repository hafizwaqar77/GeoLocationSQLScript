# 🌍 GeoLocation SQL Script Generator

This project converts **JSON data** (Countries, States, Cities) into **SQL insert scripts** for seeding your database tables in the `HR` schema.

---

## 🚀 Overview

This tool reads three JSON files:

- `Countries.json`
- `States.json`
- `Cities.json`

It then generates three SQL files:

- `Insert_Countries.sql`
- `Insert_States.sql`
- `Insert_Cities.sql`

Each SQL file includes `INSERT` statements wrapped in SQL transactions with `TRY / CATCH` blocks.

---

## 🧱 Folder Structure

📁 GeoLocationSQLScript/
│
├── Program.cs
├── Countries.json
├── States.json
├── Cities.json
├── Insert_Countries.sql ← Generated
├── Insert_States.sql ← Generated
├── Insert_Cities.sql ← Generated
└── README.md


---

## ⚙️ How to Run

### 1️⃣ Clone the Repository
```bash
git clone https://github.com/<your-username>/GeoLocationSQLScript.git
cd GeoLocationSQLScript
```
3️⃣ Run the Script

If you have .NET SDK installed:
```
dotnet run
```
Generated files:

Insert_Countries.sql
Insert_States.sql
Insert_Cities.sql


Notes

All inserts target tables under the HR schema (HR.Country, HR.State, HR.City).
Each record includes:
IsActive = 1
CreatedBy = 25
CreatedOn = GETDATE()
Cities are grouped in batches of 1000, separated by GO.
Missing or invalid records are skipped with a warning comment.
Adjust Schema according to your need 

🧰 Requirements

.NET 8 SDK or later
JSON source files (Countries.json, States.json, Cities.json)
