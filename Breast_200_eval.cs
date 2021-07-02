using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Breast_200_eval.Models;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.ComponentModel;

namespace Breast_200_eval
{
   class Program
   {
      [STAThread]
      static void Main(string[] args)
      {
         try
         {
            //TODO remove null,null in creatapplication or Eclipse above V11
            using (Application app = Application.CreateApplication(null, null))
            {
               DateTime dateTime = DateTime.Today;
               DateTime dateTime2 = DateTime.Today;
               string strtdate = string.Empty;
               string enddate = string.Empty;
               if (args.Length == 0)
               {
                  //just run 1 year worth of data
                  strtdate = dateTime.AddDays(-365).ToString("MM/dd/yyyy");
                  enddate = dateTime.ToString("MM/dd/yyyy");
               }
               else if (args.Length == 1)
               {
                  //assume start date was entered
                  strtdate = args[0];
                  enddate = DateTime.Parse(strtdate).AddDays(365).ToString("MM/dd/yyyy");
               }
               else if (args.Length == 2)
               {
                  //assume start date and enddate were entered
                  dateTime = DateTime.Parse(args[0]);
                  dateTime2 = DateTime.Parse(args[1]);
                  if (dateTime < dateTime2)
                  {
                     strtdate = dateTime.ToString("MM/dd/yyyy");
                     enddate = dateTime2.ToString("MM/dd/yyyy");
                  }
                  else
                  {
                     strtdate = dateTime2.ToString("MM/dd/yyyy");
                     enddate = dateTime.ToString("MM/dd/yyyy");
                  }
               }

               Execute(app, strtdate, enddate);
            }
         }
         catch (Exception e)
         {
            Console.Error.WriteLine(e.ToString());
            Console.WriteLine("An error was shown above. Press any key to exit...");
            Console.ReadLine();
         }
      }

      static void Execute(Application app, string strtdate, string enddate)
      {

         List<PatientModel> patients = new List<PatientModel>();
         // read csv with patient data
         // file should be stored in CSV folder in the same directory as the exe
         string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
         string sqlFolder = Path.Combine(appDirectory, "SQL");
         string sqlFile = Path.Combine(sqlFolder, "BreastPatients.sql");
         string csvFolder = Path.Combine(appDirectory, "CSV");
         string csvFile = Path.Combine(csvFolder, "Breast-Eval-heart-200-PtList.csv");
         // get patient list and information from DB
         string server = ConfigurationManager.AppSettings["server"];
         string uname = ConfigurationManager.AppSettings["uname"];
         string passwd = ConfigurationManager.AppSettings["passwd"];
         try
         {
            var process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            startInfo.FileName = "sqlcmd";
            startInfo.Arguments = $"-U {uname} -P {passwd} -S {server} -d variansystem" +
               $" -i \"{sqlFile}\" -o \"{csvFile}\"" +
               $" -h-1 -s, -W " +
               $"-v startdate = {strtdate} -v enddate = {enddate}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            process.StartInfo = startInfo;
            process.Start();

            process.WaitForExit();

         }
         catch (Exception e)
         {
            Console.WriteLine("could not start sqlcmd not installed looking for existing csv file");
         }
         using (StreamReader sr = new StreamReader(csvFile))
         {

            string currentLine;
            while ((currentLine = sr.ReadLine()) != null)
            {
               ReadCSVLine(currentLine, patients);
            }
         }
         //loop through patients, get course, and heart mean dose
         foreach (var pt in patients)
         {
            //get nstage
            pt.NStage = NStageValue(pt.StageCriteria);
            Patient patient = app.OpenPatientById(pt.Id);
            Course course = patient.Courses.First(x => x.Id == pt.CourseId);
            List<PlanSetup> plans = course.PlanSetups.Where(x => (x.ApprovalStatus.ToString() == "Completed") || (x.ApprovalStatus.ToString() == "Treatment Approved")).ToList();
            List<PlanSum> planSums = course?.PlanSums?.ToList();
            //foreach(PlanSetup plan in plans)
            //{
            //   Console.WriteLine($"{plan.ApprovalStatus.ToString()}");
            //}
            if (planSums.Count == 1)
            {
               //assume that the plan sum gives the total dose to the heart
               foreach (PlanSum planSum in planSums)
               {
                  Structure s = planSum.StructureSet.Structures.First(x => x.Id == "Heart");
                  DoseValue heartDose = planSum.GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1).MeanDose;
                  pt.MeanHeartDose = heartDose.Dose;
               }
            }
            else if (plans.Count == 1)
            {
               foreach (PlanSetup plan in plans)
               {

                  Structure s = plan.StructureSet.Structures.First(x => x.Id == "Heart");
                  DoseValue heartDose = plan.GetDVHCumulativeData(s, DoseValuePresentation.Absolute, VolumePresentation.Relative, 0.1).MeanDose;
                  pt.MeanHeartDose = heartDose.Dose;
               }
            }
            if (pt.NStage != 0) pt.MeanHeartDose = Double.NaN;
            if (pt.NStage == 0)
            {
               Console.WriteLine($"{pt.Id} {pt.LastName} MeanHeartDose: {pt.MeanHeartDose} NStage: {pt.NStage}");
            }
            app.ClosePatient();
         }
         WriteCSV(patients, csvFolder);
         Console.WriteLine($"{patients.Count}");
         Console.WriteLine("Finished...Press any key to exit...");
         Console.ReadLine();
      }

      static void WriteCSV(List<PatientModel> patients, string csvFolder)
      {
         PropertyInfo[] properties = typeof(PatientModel).GetProperties();
         string csvFileProcessed = Path.Combine(csvFolder, "Breast-Eval-heart-200-PtList-Processed.csv");
         using (StreamWriter streamWriter = new StreamWriter(csvFileProcessed))
         {

            foreach (var patient in patients)
            {
               //only write out N=0 stage patients
               if (patient.NStage == 0)
               {
                  StringBuilder stringBuilder = new StringBuilder();
                  foreach (PropertyInfo property in properties)
                  {
                     stringBuilder.Append(property.GetValue(patient, null));
                     stringBuilder.Append(',');
                  }
                  streamWriter.WriteLine(stringBuilder);
               }
            }

         }
      }


      static void ReadCSVLine(string currentLine, List<PatientModel> pms)
      {
         PatientModel pm = new PatientModel();
         try
         {
            string[] patientLine = currentLine.Split(',');
            pm.Id = patientLine[0];
            pm.LastName = patientLine[1];
            pm.FirstName = patientLine[2];
            pm.Age = Int32.Parse(patientLine[3]);
            pm.DiagnosisCode = patientLine[4];
            pm.DiagDate = patientLine[5];
            pm.CourseId = patientLine[6];
            pm.PrescriptionName = patientLine[7];
            pm.PrescriptionSite = patientLine[8];
            pm.StartDate = patientLine[9];
            pm.StageCriteria = patientLine[10];
            pm.NStage = 100;
            pm.yearFromDiagToStart = Convert.ToDouble(patientLine[12]);
            pm.MeanHeartDose = 1000;
            //Console.WriteLine($"{pm.Id} {pm.Age} {pm.CourseId}");
            pms.Add(pm);
         }
         catch
         {
            Console.WriteLine("csvline not correct format skipping...");
         }
      }
      static int NStageValue(string stagingCriteria)
      {

         if (stagingCriteria.Contains('N'))
         {
            int indexOfN = stagingCriteria.IndexOf('N', 0, stagingCriteria.Length);
            //Console.WriteLine(stagingCriteria.Substring(indexOfN+1,1));
            try
            {
               int nstage = Int32.Parse(stagingCriteria.Substring(indexOfN + 1, 1));
               return nstage;
            }
            catch (Exception e)
            {
               Console.WriteLine($"{e.Message} thrown in nstage");

            }


         }
         return -100;
      }
   }
}
