# RaceDay Event Management System

## Project Description
RaceDay is a full-stack web-based event management system designed specifically for the South African road running, walking, and cycling community.

## User Roles

### Organiser
- Create, edit, and delete events.
- Manage event categories.
- View event enrolments.
- Capture participant results.

### Participant
- Create an account and log in.
- Browse available events.
- Enter an event and select a category.
- View their own enrolments and results.

## Part 1 Deliverables
- ERD: `/docs/RaceDay_ERD.pdf`
- API Endpoint Plan: `/docs/RaceDay_API_Endpoint_Plan.pdf`
- SQL Database Script: `/docs/RaceDay_Database.sql`

## Database Setup
1. Open SQL Server Management Studio (SSMS).
2. Open the file `docs/RaceDay_Database.sql`.
3. Execute the script (F5). It will create the `RaceDay` database, all tables, constraints, and seed data.

## CI/CD
The GitHub Actions workflow (`.github/workflows/part1-ci.yml`) validates that the `/docs` folder and all required Part 1 files exist.
[<img width="1352" height="681" alt="workflow" src="https://github.com/user-attachments/assets/2ea947f3-9f9c-4520-b4a3-e57dceebdad6" />]
-

## Video Demonstration
YouTube Link: [ADD YOUR UNLISTED YOUTUBE LINK HERE]

## Repository Structure
The repository is structured as follows:

- `docs/` - Contains all Part 1 planning documents
  - `RaceDay_ERD.pdf`
  - `RaceDay_API_Endpoint_Plan.pdf`
  - `RaceDay_Database.sql`
- `.github/workflows/` - Contains the CI/CD validation workflow
- `README.md` - Project documentation

## Technologies Used
- SQL Server Management Studio (SSMS) - Database design and scripting
- Draw.io - Entity Relationship Diagram
- GitHub Actions - CI/CD validation
- Markdown - Documentation

  ## Prerequisites for Database
Before running the SQL script, ensure you have:
1. SQL Server installed (e.g., SQL Server Express or Developer).
2. SQL Server Management Studio (SSMS) installed.
3. Permission to create a new database on your local instance.

## CI/CD Pipeline Details
The GitHub Actions workflow (`part1-ci.yml`) runs on every push and pull request. 
It validates the repository structure by checking that:
- The `/docs` folder exists.
- The ERD PDF exists.
- The API Endpoint Plan PDF exists.
- The SQL script exists.
- The README exists.
This ensures that all required Part 1 deliverables are present before submission.

## Known Issues and Limitations
- The database script currently seeds with basic sample data. Advanced data validation is pending for Part 2.
- The API endpoint plan is a planning document and has not yet been implemented in code.
- The CI workflow checks for file existence but does not yet build an application (as Part 1 is planning only).

## Future Work (Part 2 & 3)
In Part 2, the RESTful API will be built using ASP.NET Core to match this endpoint plan. 
In Part 3, the MVC application will be built to consume the API, integrate Azure Blob Storage for image uploads, and be containerized using Docker.
