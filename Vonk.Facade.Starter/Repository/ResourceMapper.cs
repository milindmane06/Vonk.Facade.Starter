﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Linq;
using Vonk.Core.Common;
using Vonk.Core.Context;
using Vonk.Facade.Starter.Models;
using Vonk.Fhir.R3;

namespace Vonk.Facade.Starter.Repository
{
    public class ResourceMapper
    {
        // Mappings from ViSi model to FHIR resources 

        public IResource MapPatient(ViSiPatient source)
        {
            var patient = new Patient
            {
                Id = source.Id.ToString(),
                BirthDateElement = new Date(source.DateOfBirth.ToString("yyyy-MM-dd"))
            };
            patient.Identifier.Add(new Identifier("http://mycompany.org/patientnumber", source.PatientNumber));
            patient.Name.Add(new HumanName().WithGiven(source.FirstName).AndFamily(source.FamilyName));
            if (source.EmailAddress != null)
                patient.Telecom.Add(new ContactPoint(ContactPoint.ContactPointSystem.Email, ContactPoint.ContactPointUse.Home, source.EmailAddress));
            return patient.AsIResource();
        }

        public IResource MapBloodPressure(ViSiBloodPressure bp)
        {
            var observation = new Observation
            {
                Id = bp.Id.ToString(),
                Subject = new ResourceReference($"Patient/{bp.PatientId}"),
                Effective = new FhirDateTime(bp.MeasuredAt),
                Status = ObservationStatus.Final
            };
            observation.Category.Add(new CodeableConcept("http://hl7.org/fhir/observation-category", "vital-signs", "Vital Signs"));
            observation.Component.Add(
                new Observation.ComponentComponent()
                {
                    Code = new CodeableConcept("http://loinc.org", "8480-6", "Systolic blood pressure"),
                    Value = new Quantity(bp.Systolic, "mm[Hg]", VonkConstants.UcumSystem)
                });
            observation.Component.Add(
                new Observation.ComponentComponent()
                {
                    Code = new CodeableConcept("http://loinc.org", "8462-4", "Diastolic blood pressure"),
                    Value = new Quantity(bp.Diastolic, "mm[Hg]", VonkConstants.UcumSystem)
                });

            return observation.AsIResource();
        }

        // Mappings from FHIR resources to ViSi model

        public ViSiPatient MapViSiPatient(IResource source)
        {
            var fhirPatient = (Patient)((PocoResource)source).InnerResource;
            var visiPatient = new ViSiPatient();

            if (source.Id != null)
                visiPatient.Id = int.Parse(source.Id);

            // This code expects all of the values for the non-nullable fields of the database to be present
            // and as such is not too robust
            visiPatient.PatientNumber = fhirPatient.Identifier.Find(i => (i.System == "http://mycompany.org/patientnumber")).Value;
            visiPatient.FirstName = fhirPatient.Name.First().Given.First();
            visiPatient.FamilyName = fhirPatient.Name.First().Family;
            visiPatient.DateOfBirth = Convert.ToDateTime(fhirPatient.BirthDate);
            visiPatient.EmailAddress = fhirPatient.Telecom.Find(t => (t.System == ContactPoint.ContactPointSystem.Email))?.Value;

            return visiPatient;
        }

        public ViSiBloodPressure MapViSiBloodPressure(IResource source)
        {
            var fhirObservation = (Observation)((PocoResource)source).InnerResource;
            var visiBloodPressure = new ViSiBloodPressure();

            if (source.Id != null)
                visiBloodPressure.Id = int.Parse(source.Id);

            visiBloodPressure.PatientId = int.Parse(new ResourceIdentity(fhirObservation.Subject.Reference).Id);
            visiBloodPressure.MeasuredAt = Convert.ToDateTime(((FhirDateTime)fhirObservation.Effective).ToString());

            var systolicComponent = fhirObservation.Component.Find(c => c.Code.Coding.Exists(coding => coding.Code == "8480-6"));
            var diastolicComponent = fhirObservation.Component.Find(c => c.Code.Coding.Exists(coding => coding.Code == "8462-4"));
            visiBloodPressure.Systolic = Convert.ToInt32(((Quantity)systolicComponent.Value).Value);
            visiBloodPressure.Diastolic = Convert.ToInt32(((Quantity)diastolicComponent.Value).Value); ;

            return visiBloodPressure;
        }
    }
}
