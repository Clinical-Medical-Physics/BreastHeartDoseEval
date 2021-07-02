using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Breast_200_eval.Models
{
   public class PatientModel
   {
      public string Id { get; set; }
      public string LastName { get; set; }
      public string FirstName { get; set; }
      public int Age { get; set; }
      public string DiagnosisCode { get; set; }
      public string DiagDate { get; set; }
      public string CourseId { get; set; }
      public string PrescriptionName { get; set; }
      public string PrescriptionSite { get; set; }
      public string StartDate { get; set; }
      public string StageCriteria { get; set; }
      public int NStage { get; set; }
      public double yearFromDiagToStart { get; set; }
      public double MeanHeartDose { get; set; }

   }
}
