--The following script searches for patients that had a breast cancer diagnosis (ICD10 code)
--and treatment. The script calculates the number of years since the diagnosis and
--the start date. In our clinic we strive for less than 1 year.
--instructions below are for using SQL Server management studio
--for the ESAPI script, let the program call this script
--To Run:
--adjust the start and end date
--I run in SQL Management Studio then save the table as a csv file using Excel.
--without modifications (save start and end date) the CSV file can be used in the 
--Breast_200_eval ESAPI script to get the heart dose for all the patients returned
--
--
--enter start and end date to search for patients
declare @strtDate date, @endDate date
set @strtDate= '$(startdate)'
set @endDate = '$(enddate)'
--check is diagnosis is within 1 yr of patients start date 
declare @diagDate date,@presDate date
--change the -365 to any number of days you might be interested in
set @diagDate = dateadd(day,-365,@strtDate)
set @presDate = dateadd(day,-365,@strtDate)

--This is for a different spreadsheet
--TODO refactor this selection to match the next one
--select pPatientId,pLastName,pFirstName,AgeInYears,DiagnosisCode,DateStamp,CourseId,PrescriptionName,Site,
--StageCriteria,
--ScheduledStartTime,datediff(DD,DateStamp,ScheduledStartTime)/365.0 as yearFromDiagToStart

--for creating table that can be saved as CSV
--for CW excel sheet
select pPatientId,pLastName,pFirstName,AgeInYears,DiagnosisCode,DateStamp,replace(CourseId,',','-'),
replace(PrescriptionName,',','-'),replace(Site,',','-'),
ScheduledStartTime,replace(StageCriteria,',',';'),'',datediff(DD,DateStamp,ScheduledStartTime)/365.0 as yearFromDiagToStart
from 
(--table for connecting to new starts
select *
from
(--table for combining patient diagnosis and patient prescription
--Patient with diagnosis
select Patient.PatientId as pPatientId,Patient.LastName as pLastName,Patient.FirstName as pFirstName, --Patient
	DATEDIFF(yy,Patient.DateOfBirth,@strtDate) as AgeInYears,
	Diagnosis.DiagnosisCode,Diagnosis.DateStamp,Diagnosis.HstryUserName,Diagnosis.ObjectStatus, --Diagnosis
	PrmryDiagnosis.SummaryStage,PrmryDiagnosis.StageCriteria --Diagnosis Stage
from Patient,Diagnosis,PrmryDiagnosis
where Patient.PatientSer = Diagnosis.PatientSer
and ((Diagnosis.DiagnosisCode like 'C50%') or (Diagnosis.DiagnosisCode like 'D05%'))
and Diagnosis.DiagnosisType like 'PrmryDiagnosis'
and PrmryDiagnosis.DiagnosisSer = Diagnosis.DiagnosisSer
and Diagnosis.DateStamp between @diagDate and @endDate
and Diagnosis.ObjectStatus not like 'Deleted'
--uncomment these lines to limit by age or patient id or both
--and (DATEDIFF(yy,Patient.DateOfBirth,@strtDate) < 70)
--and Patient.PatientId = @pid
) as t inner join
(
--Patient with prescription
select Patient.PatientId,Patient.LastName,Patient.FirstName,--Patient
	Course.CourseId, --Course
	Prescription.PrescriptionName,Prescription.Site,Prescription.NumberOfFractions,Prescription.CreationDate,Prescription.Status --Prescription
from Patient,Course,TreatmentPhase,Prescription
where Patient.PatientSer =Course.PatientSer
and Course.CourseSer =TreatmentPhase.CourseSer
and TreatmentPhase.TreatmentPhaseSer = Prescription.TreatmentPhaseSer
--and (Course.StartDateTime between @strtDate and @endDate)
and (Prescription.CreationDate between @presDate and @endDate)
--uncomment these to limit prescriptions to specific fractions or ranges
--and Prescription.NumberOfFractions between 11 and 20
--and Prescription.NumberOfFractions < 20
--and Prescription.NumberOfFractions = 10
and Prescription.Status not like 'Retired'
and Prescription.Status not like 'ErrorOut'
--I have to do some logic here to get rid of cw and boosts
and (upper( Prescription.Site) like '%BREAST%')
--and (upper( Prescription.Site) like '%CHEST WALL%')
--and (upper(Prescription.PrescriptionName) like '%BREAST%')
and (upper(Prescription.PrescriptionName) not like '%BOOST%')
and (upper(Prescription.PrescriptionName) not like '%BST%')
and (upper(Prescription.PrescriptionName) not like '%CW%')
--If you want only chest walls then comment the line above with Prescription site like breast and 
--uncomment prescription site like chest wall
--you also need to comment the line below if you want chest wall
and (upper(Prescription.PrescriptionName) not like '%CHEST%')

--and Patient.PatientId = @pid
) as v on t.pPatientId=v.PatientId
) as pc inner join
(
--new starts between @strtDate and @endDate
select Patient.PatientId, Patient.LastName,Patient.FirstName, --Patient
	ScheduledActivity.ScheduledStartTime,ScheduledActivity.ActivityNote --Scheduled Activity
from Patient,ScheduledActivity,ActivityInstance,Activity
where ScheduledActivity.ActualStartDate between @strtDate and @endDate
and ScheduledActivity.ActivityInstanceSer = ActivityInstance.ActivityInstanceSer
and Activity.ActivitySer = ActivityInstance.ActivitySer
and Patient.PatientSer = ScheduledActivity.PatientSer
and ScheduledActivity.ActivityInstanceRevCount = ActivityInstance.ActivityInstanceRevCount
and Activity.ActivityCode like '%RMS%'
) as sa on pc.PatientId = sa.PatientId
order by pc.PatientId