﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Hangfire.Console;
using Hangfire.Console.Progress;
using Hangfire.Server;
using HetsData.Helpers;
using HetsData.Model;

namespace HetsImport.Import
{
    /// <summary>
    /// Import Owner Records
    /// </summary>
    public static class ImportOwner
    {
        public const string OldTable = "Owner";
        public const string NewTable = "HET_OWNER";
        public const string XmlFileName = "Owner.xml";

        /// <summary>
        /// Progress Property
        /// </summary>
        public static string OldTableProgress => OldTable + "_Progress";

        /// <summary>
        /// Generate secret keys
        /// </summary>
        /// <param name="performContext"></param>
        /// <param name="dbContext"></param>
        public static void GenerateSecretKeys(PerformContext performContext, DbAppContext dbContext)
        {
            try
            {
                performContext.WriteLine("*** Generating New Secret Keys ***");
                Debug.WriteLine("Generating New Secret Keys");

                int ii = 0;
                string _oldTableProgress = "SecretKeys_Progress";
                string _newTable = "SecretKeys";

                // check if the secret keys have already been completed
                int startPoint = ImportUtility.CheckInterMapForStartPoint(dbContext, _oldTableProgress, BcBidImport.SigId, _newTable);

                if (startPoint == BcBidImport.SigId)    // this means the assignment job is complete
                {
                    performContext.WriteLine("*** Generating New Secret Key is complete from the former process ***");
                    return;
                }

                // get records
                List<HetOwner> owners = dbContext.HetOwner.AsNoTracking()
                    .Where(x => x.BusinessId == null)
                    .ToList();

                int i = 0;
                
                foreach (HetOwner owner in owners)
                {
                    i++;
                    string key = SecretKeyHelper.RandomString(8, owner.OwnerId);

                    string temp = owner.OwnerCode;

                    if (string.IsNullOrEmpty(temp))
                    {
                        temp = SecretKeyHelper.RandomString(4, owner.OwnerId);
                    }

                    key = temp + "-" + DateTime.UtcNow.Year + "-" + key;

                    // get owner and update
                    HetOwner ownerRecord = dbContext.HetOwner.First(x => x.OwnerId == owner.OwnerId);
                    ownerRecord.SharedKey = key;

                    if (i % 500 == 0)
                    {
                        dbContext.SaveChangesForImport();
                    }

                    // save change to database
                    if (ii++ % 100 == 0)
                    {
                        try
                        {
                            Debug.WriteLine("Generating New Secret Keys - Index: " + ii);
                            ImportUtility.AddImportMapForProgress(dbContext, _oldTableProgress, ii.ToString(), BcBidImport.SigId, _newTable);
                            dbContext.SaveChangesForImport();
                        }
                        catch (Exception e)
                        {
                            performContext.WriteLine("Error saving data " + e.Message);
                        }
                    }
                }

                // ************************************************************
                // save final set of updates
                // ************************************************************
                try
                {
                    performContext.WriteLine("*** Done generating New Secret Keys ***");
                    Debug.WriteLine("Generating New Secret Keys is Done");
                    ImportUtility.AddImportMapForProgress(dbContext, _oldTableProgress, BcBidImport.SigId.ToString(), BcBidImport.SigId, _newTable);
                    dbContext.SaveChangesForImport();
                }
                catch (Exception e)
                {
                    string temp = string.Format("Error saving data (Record: {0}): {1}", ii, e.Message);
                    performContext.WriteLine(temp);
                    throw new DataException(temp);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Fix the sequence for the tables populated by the import process
        /// </summary>
        /// <param name="performContext"></param>
        /// <param name="dbContext"></param>
        public static void ResetSequence(PerformContext performContext, DbAppContext dbContext)
        {
            try
            {
                // **************************************
                // Owners
                // **************************************
                performContext.WriteLine("*** Resetting HET_OWNER database sequence after import ***");
                Debug.WriteLine("Resetting HET_OWNER database sequence after import");

                if (dbContext.HetOwner.Any())
                {
                    // get max key
                    int maxKey = dbContext.HetOwner.Max(x => x.OwnerId);
                    maxKey = maxKey + 1;

                    using (DbCommand command = dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        // check if this code already exists
                        command.CommandText = string.Format(@"SELECT SETVAL('public.""HET_OWNER_ID_seq""', {0});", maxKey);

                        dbContext.Database.OpenConnection();
                        command.ExecuteNonQuery();
                        dbContext.Database.CloseConnection();
                    }
                }

                performContext.WriteLine("*** Done resetting HET_OWNER database sequence after import ***");
                Debug.WriteLine("Resetting HET_OWNER database sequence after import - Done!");

                // **************************************
                // Contacts
                // **************************************
                performContext.WriteLine("*** Resetting HET_CONTACT database sequence after import ***");
                Debug.WriteLine("Resetting HET_CONTACT database sequence after import");

                if (dbContext.HetContact.Any())
                {
                    // get max key
                    int maxKey = dbContext.HetContact.Max(x => x.ContactId);
                    maxKey = maxKey + 1;

                    using (DbCommand command = dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        // check if this code already exists
                        command.CommandText = string.Format(@"SELECT SETVAL('public.""HET_CONTACT_ID_seq""', {0});", maxKey);

                        dbContext.Database.OpenConnection();
                        command.ExecuteNonQuery();
                        dbContext.Database.CloseConnection();
                    }
                }

                performContext.WriteLine("*** Done resetting HET_CONTACT database sequence after import ***");
                Debug.WriteLine("Resetting HET_CONTACT database sequence after import - Done!");

                // **************************************
                // Notes
                // **************************************
                performContext.WriteLine("*** Resetting HET_NOTE database sequence after import ***");
                Debug.WriteLine("Resetting HET_NOTE database sequence after import");

                if (dbContext.HetNote.Any())
                {
                    // get max key
                    int maxKey = dbContext.HetNote.Max(x => x.NoteId);
                    maxKey = maxKey + 1;

                    using (DbCommand command = dbContext.Database.GetDbConnection().CreateCommand())
                    {
                        // check if this code already exists
                        command.CommandText = string.Format(@"SELECT SETVAL('public.""HET_NOTE_ID_seq""', {0});", maxKey);

                        dbContext.Database.OpenConnection();
                        command.ExecuteNonQuery();
                        dbContext.Database.CloseConnection();
                    }
                }

                performContext.WriteLine("*** Done resetting HET_NOTE database sequence after import ***");
                Debug.WriteLine("Resetting HET_NOTE database sequence after import - Done!");
            }
            catch (Exception e)
            {
                performContext.WriteLine("*** ERROR ***");
                performContext.WriteLine(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Fix the primary contact foreign keys
        /// </summary>
        /// <param name="performContext"></param>
        /// <param name="dbContext"></param>
        public static void FixPrimaryContacts(PerformContext performContext, DbAppContext dbContext)
        {
            try
            {
                int ii = 0;

                performContext.WriteLine("*** Resetting HET_OWNER contact foreign keys ***");
                Debug.WriteLine("Resetting HET_OWNER contact foreign keys");

                IQueryable<HetOwner> owners = dbContext.HetOwner
                    .Include(x => x.PrimaryContact)
                    .Where(x => x.PrimaryContactId != null);

                foreach (HetOwner owner in owners)
                {
                    int? contactId = owner.PrimaryContactId;

                    if (contactId != null)
                    {
                        // get contact and update
                        HetContact contact = dbContext.HetContact.FirstOrDefault(x => x.ContactId == contactId);

                        if (owner.HetContact == null)
                        {
                            owner.HetContact = new List<HetContact>();
                        }

                        owner.HetContact.Add(contact);
                        dbContext.HetOwner.Update(owner);
                    }

                    // save change to database periodically to avoid frequent writing to the database
                    if (++ii % 500 == 0)
                    {
                        dbContext.SaveChangesForImport();                        
                    }
                }

                // save last batch
                dbContext.SaveChangesForImport();
                

                performContext.WriteLine("*** Done resetting HET_OWNER contact foreign keys ***");
                Debug.WriteLine("Resetting HET_OWNER contact foreign keys - Done!");                
            }
            catch (Exception e)
            {
                performContext.WriteLine("*** ERROR ***");
                performContext.WriteLine(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Import Owner Records
        /// </summary>
        /// <param name="performContext"></param>
        /// <param name="dbContext"></param>
        /// <param name="fileLocation"></param>
        /// <param name="systemId"></param>
        public static void Import(PerformContext performContext, DbAppContext dbContext, string fileLocation, string systemId)
        {
            // check the start point. If startPoint == sigId then it is already completed
            int startPoint = ImportUtility.CheckInterMapForStartPoint(dbContext, OldTableProgress, BcBidImport.SigId, NewTable);

            if (startPoint == BcBidImport.SigId)    // this means the import job it has done today is complete for all the records in the xml file.    // This means the import job it has done today is complete for all the records in the xml file.
            {
                performContext.WriteLine("*** Importing " + XmlFileName + " is complete from the former process ***");
                return;
            }

            int maxOwnerIndex = 0;

            if (dbContext.HetOwner.Any())
            {
                maxOwnerIndex = dbContext.HetOwner.Max(x => x.OwnerId);
            }
            
            try
            {
                string rootAttr = "ArrayOf" + OldTable;

                // create progress indicator
                performContext.WriteLine("Processing " + OldTable);
                IProgressBar progress = performContext.WriteProgressBar();
                progress.SetValue(0);

                // create serializer and serialize xml file
                XmlSerializer ser = new XmlSerializer(typeof(ImportModels.Owner[]), new XmlRootAttribute(rootAttr));
                MemoryStream memoryStream = ImportUtility.MemoryStreamGenerator(XmlFileName, OldTable, fileLocation, rootAttr);
                ImportModels.Owner[] legacyItems = (ImportModels.Owner[])ser.Deserialize(memoryStream);

                int ii = startPoint;

                // skip the portion already processed
                if (startPoint > 0)
                {
                    legacyItems = legacyItems.Skip(ii).ToArray();
                }

                Debug.WriteLine("Importing Owner Data. Total Records: " + legacyItems.Length);

                foreach (ImportModels.Owner item in legacyItems.WithProgress(progress))
                {
                    // see if we have this one already.
                    HetImportMap importMap = dbContext.HetImportMap.AsNoTracking()
                        .FirstOrDefault(x => x.OldTable == OldTable && 
                                             x.OldKey == item.Popt_Id.ToString());

                    // new entry
                    if (importMap == null)
                    {
                        HetOwner owner = null;
                        CopyToInstance(dbContext, item, ref owner, systemId, ref maxOwnerIndex);
                        ImportUtility.AddImportMap(dbContext, OldTable, item.Popt_Id.ToString(), NewTable, owner.OwnerId);
                    }

                    // save change to database periodically to avoid frequent writing to the database
                    if (++ii % 1000 == 0) 
                    {
                        try
                        {
                            ImportUtility.AddImportMapForProgress(dbContext, OldTableProgress, ii.ToString(), BcBidImport.SigId, NewTable);
                            dbContext.SaveChangesForImport();
                        }
                        catch (Exception e)
                        {
                            string temp = string.Format("Error saving data (OwnerIndex: {0}): {1}", maxOwnerIndex, e.Message);
                            performContext.WriteLine(temp);
                            throw new DataException(temp);
                        }
                    }
                }

                try
                {
                    performContext.WriteLine("*** Importing " + XmlFileName + " is Done ***");
                    ImportUtility.AddImportMapForProgress(dbContext, OldTableProgress, BcBidImport.SigId.ToString(), BcBidImport.SigId, NewTable);
                    dbContext.SaveChangesForImport();
                }
                catch (Exception e)
                {
                    string temp = string.Format("Error saving data (OwnerIndex: {0}): {1}", maxOwnerIndex, e.Message);
                    performContext.WriteLine(temp);
                    throw new DataException(temp);
                }
            }
            catch (Exception e)
            {
                performContext.WriteLine("*** ERROR ***");
                performContext.WriteLine(e.ToString());
                throw;
            }                       
        }

        /// <summary>
        /// Map data
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="oldObject"></param>
        /// <param name="owner"></param>
        /// <param name="systemId"></param>
        /// <param name="maxOwnerIndex"></param>
        private static void CopyToInstance(DbAppContext dbContext, ImportModels.Owner oldObject, 
            ref HetOwner owner, string systemId, ref int maxOwnerIndex)
        {
            try
            {
                if (owner != null)
                {
                    return;
                }

                owner = new HetOwner { OwnerId = ++maxOwnerIndex};
                
                // ***********************************************
                // set owner code
                // ***********************************************
                string tempOwnerCode = ImportUtility.CleanString(oldObject.Owner_Cd).ToUpper();

                if (!string.IsNullOrEmpty(tempOwnerCode))
                {
                    owner.OwnerCode = tempOwnerCode;
                }
                else
                {
                    // must have an owner code: HETS-817
                    return;
                }

                // ***********************************************
                // set company name
                // ***********************************************
                string tempCompanyName = ImportUtility.CleanString(oldObject.Company_Name);

                if (!string.IsNullOrEmpty(tempCompanyName))
                {
                    tempCompanyName = ImportUtility.GetCapitalCase(tempCompanyName);

                    // fix some capital case names
                    tempCompanyName = tempCompanyName.Replace("Bc", "BC");
                    tempCompanyName = tempCompanyName.Replace("Rv", "RV");
                    tempCompanyName = tempCompanyName.Replace(", & ", " & ");

                    owner.OrganizationName = tempCompanyName;
                }

                // ***********************************************
                // DBA Name
                // ***********************************************
                string tempDbaName = ImportUtility.CleanString(oldObject.Operating_AS);

                if (!string.IsNullOrEmpty(tempDbaName))
                {
                    tempDbaName = ImportUtility.GetCapitalCase(tempDbaName);
                    owner.DoingBusinessAs = tempDbaName;
                }

                // ***********************************************
                // maintenance contractor
                // ***********************************************
                if (oldObject.Maintenance_Contractor != null)
                {
                    owner.IsMaintenanceContractor = (oldObject.Maintenance_Contractor.Trim() == "Y");
                }

                // ***********************************************
                // residency
                // ***********************************************
                if (oldObject.Local_To_Area != null)
                {
                    owner.MeetsResidency = (oldObject.Local_To_Area.Trim() == "Y");
                }

                // ***********************************************
                // set local area
                // ***********************************************                
                HetLocalArea localArea = dbContext.HetLocalArea.FirstOrDefault(x => x.LocalAreaId == oldObject.Area_Id);

                if (localArea != null)
                {
                    owner.LocalAreaId = localArea.LocalAreaId;
                }

                // ***********************************************
                // set other attributes
                // ***********************************************   
                owner.WorkSafeBcexpiryDate = ImportUtility.CleanDate(oldObject.CGL_End_Dt);
                owner.CglendDate = ImportUtility.CleanDate(oldObject.CGL_End_Dt);

                owner.WorkSafeBcpolicyNumber = ImportUtility.CleanString(oldObject.WCB_Num);

                if (owner.WorkSafeBcpolicyNumber == "0")
                {
                    owner.WorkSafeBcpolicyNumber = null;
                }

                string tempCglCompanyName = ImportUtility.CleanString(oldObject.CGL_Company);
                tempCglCompanyName = ImportUtility.GetCapitalCase(tempCglCompanyName);                

                if (!string.IsNullOrEmpty(tempCglCompanyName))
                {
                    owner.CglCompanyName = tempCglCompanyName;
                }                

                owner.CglPolicyNumber = ImportUtility.CleanString(oldObject.CGL_Policy);

                if (owner.CglPolicyNumber == "")
                {
                    owner.CglPolicyNumber = null;
                }
                
                // ***********************************************
                // manage archive and owner status
                // ***********************************************
                string tempArchive = oldObject.Archive_Cd;
                string tempStatus = oldObject.Status_Cd.Trim();

                if (tempArchive == "Y")
                {
                    int? statusId = StatusHelper.GetStatusId("Archived", "ownerStatus", dbContext);

                    if (statusId == null)
                    {
                        throw new DataException(string.Format("Status Id cannot be null (OwnerIndex: {0}", maxOwnerIndex));
                    }

                    // archived!
                    owner.ArchiveCode = "Y";
                    owner.ArchiveDate = DateTime.UtcNow;
                    owner.ArchiveReason = "Imported from BC Bid";
                    owner.OwnerStatusTypeId = (int)statusId;

                    string tempArchiveReason = ImportUtility.CleanString(oldObject.Archive_Reason);

                    if (!string.IsNullOrEmpty(tempArchiveReason))
                    {
                        owner.ArchiveReason = ImportUtility.GetUppercaseFirst(tempArchiveReason);
                    }
                }
                else
                {
                    int? statusId = StatusHelper.GetStatusId("Approved", "ownerStatus", dbContext);

                    if (statusId == null)
                    {
                        throw new DataException(string.Format("Status Id cannot be null (OwnerIndex: {0}", maxOwnerIndex));
                    }

                    int? statusIdUnapproved = StatusHelper.GetStatusId("Unapproved", "ownerStatus", dbContext);

                    if (statusIdUnapproved == null)
                    {
                        throw new DataException(string.Format("Status Id cannot be null (OwnerIndex: {0}", maxOwnerIndex));
                    }

                    owner.ArchiveCode = "N";
                    owner.ArchiveDate = null;
                    owner.ArchiveReason = null;
                    owner.OwnerStatusTypeId = tempStatus == "A" ? (int)statusId : (int)statusIdUnapproved;
                    owner.StatusComment = string.Format("Imported from BC Bid ({0})", tempStatus);
                }

                // ***********************************************
                // manage owner information
                // ***********************************************
                string tempOwnerFirstName = ImportUtility.CleanString(oldObject.Owner_First_Name);
                tempOwnerFirstName = ImportUtility.GetCapitalCase(tempOwnerFirstName);

                string tempOwnerLastName = ImportUtility.CleanString(oldObject.Owner_Last_Name);
                tempOwnerLastName = ImportUtility.GetCapitalCase(tempOwnerLastName);

                // some fields have duplicates in the name
                if (tempOwnerLastName == tempOwnerFirstName)
                {
                    tempOwnerFirstName = "";
                }

                if (!string.IsNullOrEmpty(tempOwnerLastName))
                {
                    owner.Surname = tempOwnerLastName;
                }

                if (!string.IsNullOrEmpty(tempOwnerFirstName))
                {
                    owner.GivenName = tempOwnerFirstName;
                }                

                // if there is no company name - create one
                string tempName = "";

                if (string.IsNullOrEmpty(tempCompanyName) &&
                    !string.IsNullOrEmpty(owner.OwnerCode) &&
                    string.IsNullOrEmpty(tempOwnerLastName))
                {
                    owner.OrganizationName = owner.OwnerCode;
                    tempName = owner.OrganizationName;
                }
                else if (string.IsNullOrEmpty(tempCompanyName) && !string.IsNullOrEmpty(owner.OwnerCode))
                {
                    owner.OrganizationName = string.Format("{0} - {1} {2}", owner.OwnerCode, tempOwnerFirstName, tempOwnerLastName);
                    tempName = owner.OrganizationName;
                }
                else if (string.IsNullOrEmpty(tempCompanyName))
                {
                    owner.OrganizationName = string.Format("{0} {1}", tempOwnerFirstName, tempOwnerLastName);
                    tempName = owner.OrganizationName;
                }                     

                // check if the organization name is actually a "Ltd" or other company name
                // (in BC Bid the names are sometimes used to store the org)
                if (!string.IsNullOrEmpty(tempName) &&
                    tempName.IndexOf(" Ltd", StringComparison.Ordinal) > -1 ||
                    tempName.IndexOf(" Resort", StringComparison.Ordinal) > -1 ||
                    tempName.IndexOf(" Oilfield", StringComparison.Ordinal) > -1 ||
                    tempName.IndexOf(" Nation", StringComparison.Ordinal) > -1 ||
                    tempName.IndexOf(" Ventures", StringComparison.Ordinal) > -1 ||
                    tempName.IndexOf(" Indian Band", StringComparison.Ordinal) > -1)
                {
                    owner.Surname = null;
                    owner.GivenName = null;

                    if (!string.IsNullOrEmpty(owner.OwnerCode))
                    {
                        owner.OrganizationName = string.Format("{0} {1}", tempOwnerFirstName, tempOwnerLastName);
                    }
                }

                // ***********************************************
                // format company names
                // ***********************************************
                if (owner.OrganizationName.EndsWith(" Ltd") ||
                    owner.OrganizationName.EndsWith(" Ulc") ||
                    owner.OrganizationName.EndsWith(" Inc") ||
                    owner.OrganizationName.EndsWith(" Co"))
                {
                    owner.OrganizationName = owner.OrganizationName + ".";
                }

                // ***********************************************
                // manage contacts
                // ***********************************************
                bool contactExists = false;

                if (owner.HetContact != null)
                {
                    foreach (HetContact contactItem in owner.HetContact)
                    {
                        if (!string.Equals(contactItem.GivenName, tempOwnerFirstName, StringComparison.InvariantCultureIgnoreCase) &&
                            !string.Equals(contactItem.Surname, tempOwnerLastName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            contactExists = true;
                        }
                    }
                }

                string tempPhone = ImportUtility.FormatPhone(oldObject.Ph_Country_Code, oldObject.Ph_Area_Code, oldObject.Ph_Number, oldObject.Ph_Extension);
                string tempFax = ImportUtility.FormatPhone(oldObject.Fax_Country_Code, oldObject.Fax_Area_Code, oldObject.Fax_Number, oldObject.Fax_Extension);
                string tempMobile = ImportUtility.FormatPhone(oldObject.Cell_Country_Code, oldObject.Cell_Area_Code, oldObject.Cell_Number, oldObject.Cell_Extension);

                // only add if they don't already exist
                if (!contactExists && !string.IsNullOrEmpty(tempOwnerLastName))
                {                    
                    HetContact ownerContact = new HetContact
                    {                        
                        Role = "Owner",
                        Province = "BC",
                        Notes = "",
                        WorkPhoneNumber = tempPhone,
                        MobilePhoneNumber = tempMobile,
                        FaxPhoneNumber = tempFax,
                        AppCreateUserid = systemId,
                        AppCreateTimestamp = DateTime.UtcNow,
                        AppLastUpdateUserid = systemId,
                        AppLastUpdateTimestamp = DateTime.UtcNow
                    };

                    if (!string.IsNullOrEmpty(tempOwnerLastName))
                    {
                        ownerContact.Surname = tempOwnerLastName;
                    }

                    if (!string.IsNullOrEmpty(tempOwnerFirstName))
                    {
                        ownerContact.GivenName = tempOwnerFirstName;
                    }                    

                    if (owner.HetContact == null)
                    {
                        owner.HetContact = new List<HetContact>();
                    }
                    
                    owner.HetContact.Add(ownerContact);
                }

                // is the BC Bid contact the same as the owner?
                string tempContactPerson = ImportUtility.CleanString(oldObject.Contact_Person);
                tempContactPerson = ImportUtility.GetCapitalCase(tempContactPerson);

                if (!string.IsNullOrEmpty(tempContactPerson))
                {
                    string tempContactPhone = ImportUtility.FormatPhone(oldObject.Contact_Country_Code, oldObject.Contact_Area_Code, oldObject.Contact_Number, oldObject.Contact_Extension);

                    // split the name
                    string tempContactFirstName;
                    string tempContactLastName;

                    if (tempContactPerson.IndexOf(" Or ", StringComparison.Ordinal) > -1)
                    {
                        tempContactFirstName = tempContactPerson;
                        tempContactLastName = "";
                    }
                    else
                    {
                        tempContactFirstName = ImportUtility.GetNamePart(tempContactPerson, 1);
                        tempContactLastName = ImportUtility.GetNamePart(tempContactPerson, 2);
                    }

                    // check if the name is unique
                    if ((!string.Equals(tempContactFirstName, tempOwnerFirstName, StringComparison.InvariantCultureIgnoreCase) &&
                         !string.Equals(tempContactLastName, tempOwnerLastName, StringComparison.InvariantCultureIgnoreCase)) ||
                        (!string.Equals(tempContactFirstName, tempOwnerFirstName, StringComparison.InvariantCultureIgnoreCase) &&
                         !string.Equals(tempContactFirstName, tempOwnerLastName, StringComparison.InvariantCultureIgnoreCase) &&
                         !string.IsNullOrEmpty(tempContactLastName)))
                    {
                        // check if the name(s) already exist
                        contactExists = false;

                        if (owner.HetContact != null)
                        {
                            foreach (HetContact contactItem in owner.HetContact)
                            {
                                if (string.Equals(contactItem.GivenName, tempContactFirstName, StringComparison.InvariantCultureIgnoreCase) &&
                                    string.Equals(contactItem.Surname, tempContactLastName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    contactExists = true;
                                }
                            }
                        }

                        if (!contactExists)
                        {
                            string tempPhoneNumber = "";

                            if (tempPhone != tempContactPhone && !string.IsNullOrEmpty(tempContactPhone))
                            {
                                tempPhoneNumber = tempContactPhone;
                            }

                            HetContact primaryContact = new HetContact
                            {
                                Role = "Primary Contact",
                                Province = "BC",           
                                WorkPhoneNumber = tempPhone,
                                MobilePhoneNumber = tempMobile,
                                FaxPhoneNumber = tempFax,
                                Notes = tempPhoneNumber,
                                AppCreateUserid = systemId,
                                AppCreateTimestamp = DateTime.UtcNow,
                                AppLastUpdateUserid = systemId,
                                AppLastUpdateTimestamp = DateTime.UtcNow
                            };

                            if (!string.IsNullOrEmpty(tempContactLastName))
                            {
                                primaryContact.Surname = tempContactLastName;
                            }

                            if (!string.IsNullOrEmpty(tempContactFirstName))
                            {
                                primaryContact.GivenName = tempContactFirstName;
                            }

                            string tempComment = ImportUtility.CleanString(oldObject.Comment);

                            if (!string.IsNullOrEmpty(tempComment))
                            {
                                tempComment = ImportUtility.GetCapitalCase(tempComment);
                                primaryContact.Notes = tempComment;
                            }
                                                        
                            owner.PrimaryContact = primaryContact; // since this was the default in the file - make it primary
                        }
                    }
                }

                // ensure the contact is valid
                if (owner.HetContact != null)
                {
                    for (int i = owner.HetContact.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(owner.HetContact.ElementAt(i).GivenName) &&
                            string.IsNullOrEmpty(owner.HetContact.ElementAt(i).Surname))
                        {
                            owner.HetContact.Remove(owner.HetContact.ElementAt(i));
                        }
                    }

                    // update primary
                    if (owner.HetContact.Count > 0 &&
                        owner.PrimaryContact == null)
                    {
                        owner.PrimaryContact = owner.HetContact.ElementAt(0);
                        owner.HetContact.Remove(owner.HetContact.ElementAt(0));
                    }
                }

                // ***********************************************
                // create owner
                // ***********************************************                            
                owner.AppCreateUserid = systemId;
                owner.AppCreateTimestamp = DateTime.UtcNow;
                owner.AppLastUpdateUserid = systemId;
                owner.AppLastUpdateTimestamp = DateTime.UtcNow;

                dbContext.HetOwner.Add(owner);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("***Error*** - Owner Code: " + owner.OwnerCode);
                Debug.WriteLine("***Error*** - Master Owner Index: " + maxOwnerIndex);
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public static void Obfuscate(PerformContext performContext, DbAppContext dbContext, string sourceLocation, string destinationLocation, string systemId)
        {
            int startPoint = ImportUtility.CheckInterMapForStartPoint(dbContext, "Obfuscate_" + OldTableProgress, BcBidImport.SigId, NewTable);

            if (startPoint == BcBidImport.SigId)    // this means the import job it has done today is complete for all the records in the xml file.
            {
                performContext.WriteLine("*** Obfuscating " + XmlFileName + " is complete from the former process ***");
                return;
            }
            try
            {
                string rootAttr = "ArrayOf" + OldTable;

                // create progress indicator
                performContext.WriteLine("Processing " + OldTable);
                IProgressBar progress = performContext.WriteProgressBar();
                progress.SetValue(0);

                // create serializer and serialize xml file
                XmlSerializer ser = new XmlSerializer(typeof(ImportModels.Owner[]), new XmlRootAttribute(rootAttr));
                MemoryStream memoryStream = ImportUtility.MemoryStreamGenerator(XmlFileName, OldTable, sourceLocation, rootAttr);
                ImportModels.Owner[] legacyItems = (ImportModels.Owner[])ser.Deserialize(memoryStream);

                performContext.WriteLine("Obfuscating owner data");
                progress.SetValue(0);

                int currentOwner = 0;

                List<ImportMapRecord> importMapRecords = new List<ImportMapRecord>();

                foreach (ImportModels.Owner item in legacyItems.WithProgress(progress))
                {
                    item.Created_By = systemId;

                    if (item.Modified_By != null)
                    {
                        item.Modified_By = systemId;
                    }

                    ImportMapRecord importMapRecordOrganization = new ImportMapRecord
                    {
                        TableName = NewTable,
                        MappedColumn = "OrganizationName",
                        OriginalValue = item.CGL_Company,
                        NewValue = "Company " + currentOwner
                    };

                    importMapRecords.Add(importMapRecordOrganization);

                    ImportMapRecord importMapRecordFirstName = new ImportMapRecord
                    {
                        TableName = NewTable,
                        MappedColumn = "Owner_First_Name",
                        OriginalValue = item.Owner_First_Name,
                        NewValue = "OwnerFirst" + currentOwner
                    };

                    importMapRecords.Add(importMapRecordFirstName);

                    ImportMapRecord importMapRecordLastName = new ImportMapRecord
                    {
                        TableName = NewTable,
                        MappedColumn = "Owner_Last_Name",
                        OriginalValue = item.Owner_Last_Name,
                        NewValue = "OwnerLast" + currentOwner
                    };

                    importMapRecords.Add(importMapRecordLastName);

                    ImportMapRecord importMapRecordOwnerCode = new ImportMapRecord
                    {
                        TableName = NewTable,
                        MappedColumn = "Owner_Cd",
                        OriginalValue = item.Owner_Cd,
                        NewValue = "OO" + currentOwner
                    };

                    importMapRecords.Add(importMapRecordOwnerCode);

                    item.Owner_Cd = "OO" + currentOwner;
                    item.Owner_First_Name = "OwnerFirst" + currentOwner;
                    item.Owner_Last_Name = "OwnerLast" + currentOwner;
                    item.Contact_Person = ImportUtility.ScrambleString(ImportUtility.CleanString(item.Contact_Person));
                    item.Comment = ImportUtility.ScrambleString(ImportUtility.CleanString(item.Comment));
                    item.WCB_Num = ImportUtility.ScrambleString(ImportUtility.CleanString(item.WCB_Num));
                    item.CGL_Company = ImportUtility.ScrambleString(ImportUtility.CleanString(item.CGL_Company));
                    item.CGL_Policy = ImportUtility.ScrambleString(ImportUtility.CleanString(item.CGL_Policy));

                    currentOwner++;
                }
                
                performContext.WriteLine("Writing " + XmlFileName + " to " + destinationLocation);

                // write out the array
                FileStream fs = ImportUtility.GetObfuscationDestination(XmlFileName, destinationLocation);
                ser.Serialize(fs, legacyItems);
                fs.Close();

                // write out the spreadsheet of import records
                ImportUtility.WriteImportRecordsToExcel(destinationLocation, importMapRecords, OldTable);
            }
            catch (Exception e)
            {
                performContext.WriteLine("*** ERROR ***");
                performContext.WriteLine(e.ToString());
                throw;
            }
        }
    }
}


