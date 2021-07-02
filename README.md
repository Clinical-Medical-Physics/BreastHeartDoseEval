# Breast Heart Dose Eval
ESAPI Script to evaluate mean heart dose for all breast patients.

## Installation
To run this script you will need to have SQL Server Managment Studio (sqlcmd) installed on the workstation that runs the script.
If you do not have sqlcmd installed the script will look for a file name Breast-Eval-heart-200-PtList.csv in the CSV folder where
the exe file risides. You can generate the csv file using the provided BreastPatients.sql script in the SQL folder where the exe file resides. 

## First Use
Before compiling, you must rename the app.config.sameple the file to app.config and modify the variables in that file. I am using V11 or Aria and have
a reports user and password that works. For newer versions, you may have to get the reports username from Varian if your own credentials to not work.

## Output
A CSV file with the PatientModel list is output. The last column in that file is the mean dose to the heart for that patient.

## Caveats
I have not tried to account for centers having vastly different planning styles. In our clinic a typical breast plan will be either a single plan,
a plan with a boost, a plan with nodes, etc. To account for this variation I am only looking for patients that have a single plan sum, or a single
plan. That is where I get the heart dose from. I have also limited the results to only those patients that have a N=0 stage in their staging summary.