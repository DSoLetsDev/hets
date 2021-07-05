﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HetsData.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HetsData.Helpers
{
    #region Rental Request Models

    public class RentalRequestLite
    {
        public int Id { get; set; }
        public HetLocalArea LocalArea { get; set; }
        public int? EquipmentCount { get; set; }
        public string EquipmentTypeName { get; set; }
        public string DistrictEquipmentName { get; set; }
        public string ProjectName { get; set; }
        public HetContact PrimaryContact { get; set; }
        public string Status { get; set; }
        public int? ProjectId { get; set; }
        public DateTime? ExpectedStartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public int YesCount { get; set; }
    }

    public class RentalRequestHires
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int EquipmentId { get; set; }
        public string LocalAreaName { get; set; }
        public int ServiceAreaId { get; set; }
        public string OwnerCode { get; set; }
        public string CompanyName { get; set; }
        public string EquipmentCode { get; set; }
        public string EquipmentPrefix { get; set; }
        public int EquipmentNumber { get; set; }
        public string EquipmentMake { get; set; }
        public string EquipmentModel { get; set; }
        public string EquipmentSize { get; set; }
        public string EquipmentYear { get; set; }
        public int ProjectId { get; set; }
        public string ProjectNumber { get; set; }
        public DateTime? NoteDate { get; set; }
        public string NoteType { get; set; }
        public string Reason { get; set; }
        public string OfferResponseNote { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }

    #endregion

    public static class RentalRequestHelper
    {
        #region Get Rental Request Record

        /// <summary>
        /// Get rental request record
        /// </summary>
        /// <param name="id"></param>
        /// <param name="context"></param>
        public static HetRentalRequest GetRecord(int id, DbAppContext context)
        {
            HetRentalRequest request = context.HetRentalRequest.AsNoTracking()
                .Include(x => x.RentalRequestStatusType)
                .Include(x => x.LocalArea.ServiceArea.District.Region)
                .Include(x => x.Project)
                    .ThenInclude(c => c.PrimaryContact)
                .Include(x => x.Project)
                    .ThenInclude(c => c.ProjectStatusType)
                .Include(x => x.HetRentalRequestAttachment)
                .Include(x => x.DistrictEquipmentType)
                .Include(x => x.HetRentalRequestRotationList)
                    .ThenInclude(y => y.Equipment)
                        .ThenInclude(z => z.EquipmentStatusType)
                .FirstOrDefault(a => a.RentalRequestId == id);

            if (request != null)
            {
                request.Status = request.RentalRequestStatusType.RentalRequestStatusTypeCode;

                // calculate the Yes Count based on the RentalRequestList
                request.YesCount = CalculateYesCount(request);

                // calculate YTD hours for the equipment records
                if (request.HetRentalRequestRotationList != null)
                {
                    foreach (HetRentalRequestRotationList rotationList in request.HetRentalRequestRotationList)
                    {
                        if (rotationList.Equipment != null)
                        {
                            rotationList.Equipment.HoursYtd = EquipmentHelper.GetYtdServiceHours(rotationList.Equipment.EquipmentId, context);
                        }
                    }
                }
            }

            return request;
        }

        /// <summary>
        /// Get rental request record
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scoringRules"></param>
        /// <param name="context"></param>
        public static HetRentalRequest GetRecordWithRotationList(int id, SeniorityScoringRules scoringRules, DbAppContext context)
        {
            //load up the rental request with the equipment decoupled
            HetRentalRequest request = context.HetRentalRequest.AsNoTracking()
                .Include(x => x.DistrictEquipmentType)
                    .ThenInclude(y => y.EquipmentType)
                .Include(x => x.FirstOnRotationList)
                .Include(x => x.HetRentalRequestAttachment)
                .Include(x => x.HetRentalRequestRotationList)
                .FirstOrDefault(a => a.RentalRequestId == id);
             
            //determine if the request is complete or not
            HetRentalRequestStatusType status = context.HetRentalRequestStatusType.AsNoTracking()
                .Where(x => x.RentalRequestStatusTypeCode == "Complete").FirstOrDefault();
            bool isCompletedRequest = (request.RentalRequestStatusTypeId == status.RentalRequestStatusTypeId) ? true : false;
            
            //pull out the date that request was last updated
            var requestDate = request.AppLastUpdateTimestamp;

            foreach (var rrrl in request.HetRentalRequestRotationList)
            {
                //get the equipment id from the rental request rotation list item (mouthful!)
                var equipmentId = rrrl.EquipmentId;

                HetEquipment equipment = null;
                //the request is completed so lets dig thru the equipment history to pull out the 
                // data as it was when the rental request was made
                if (isCompletedRequest)
                {
                    //get the equipment history records that are prior to the request last update
                    var equipmentHist = context.HetEquipmentHist.AsNoTracking()
                        .Where(x => x.EquipmentId == equipmentId && x.AppLastUpdateTimestamp <= requestDate)
                        .OrderByDescending(x => x.AppLastUpdateTimestamp);

                    //get the max id list, this ensures we get the oldest record in case there are many records with the same time
                    var foundHistItem = equipmentHist
                        .Where(x => x.EquipmentHistId == equipmentHist.Max(m => m.EquipmentHistId))
                        .FirstOrDefault();
                    
                    // there is an explicit operator conversion in the HetsData.Extension.HetEquipmentExtension class
                    equipment = (HetEquipment)foundHistItem;
                }
                else
                {
                    // it's in progress or being created so pull current equipment data
                    equipment = context.HetEquipment.AsNoTracking()
                        .Where(x => x.EquipmentId == equipmentId)
                        .FirstOrDefault();
                }
                
                //lets make sure we actually have an equipment object
                if (equipment != null)
                {
                    //assign the equipment data into the rotation hire list
                    rrrl.Equipment = equipment;

                    //some queries to pull the rest of the data related to equipment.. not historical (is that a potential issue?)
                    rrrl.Equipment.HetEquipmentAttachment = context.HetEquipmentAttachment.AsNoTracking()
                        .Where(x => x.EquipmentId == equipmentId).ToList();
                    
                    rrrl.Equipment.LocalArea = context.HetLocalArea.AsNoTracking()
                        .Where(x => x.LocalAreaId == rrrl.Equipment.LocalAreaId).FirstOrDefault();

                    rrrl.Equipment.DistrictEquipmentType = context.HetDistrictEquipmentType.AsNoTracking()
                        .Where(x => x.DistrictEquipmentTypeId == rrrl.Equipment.DistrictEquipmentTypeId)
                        .Include(y => y.EquipmentType).FirstOrDefault();

                    rrrl.Equipment.Owner = context.HetOwner.AsNoTracking()
                        .Where(x => x.OwnerId == rrrl.Equipment.OwnerId)
                        .Include(y => y.PrimaryContact).FirstOrDefault();
                }
            }
            
            /* -- original code to pull the request, rotation list and equipment
            HetRentalRequest request = context.HetRentalRequest.AsNoTracking()
                .Include(x => x.DistrictEquipmentType)
                    .ThenInclude(y => y.EquipmentType)
                .Include(x => x.FirstOnRotationList)
                .Include(x => x.HetRentalRequestAttachment)
                .Include(x => x.HetRentalRequestRotationList)
                    .ThenInclude(y => y.Equipment)
                        .ThenInclude(r => r.HetEquipmentAttachment)
                .Include(x => x.HetRentalRequestRotationList)
                    .ThenInclude(y => y.Equipment)
                        .ThenInclude(r => r.LocalArea)
                .Include(x => x.HetRentalRequestRotationList)
                    .ThenInclude(y => y.Equipment)
                        .ThenInclude(r => r.DistrictEquipmentType)
                .Include(x => x.HetRentalRequestRotationList)
                    .ThenInclude(y => y.Equipment)
                        .ThenInclude(r => r.DistrictEquipmentType)
                            .ThenInclude(y => y.EquipmentType)
                .Include(x => x.HetRentalRequestRotationList)
                    .ThenInclude(y => y.Equipment)
                        .ThenInclude(e => e.Owner)
                            .ThenInclude(c => c.PrimaryContact)
                .FirstOrDefault(a => a.RentalRequestId == id);*/

            if (request != null)
            {
                // re-sort list using: LocalArea / District Equipment Type and SenioritySortOrder (desc)
                request.HetRentalRequestRotationList = request.HetRentalRequestRotationList
                    .OrderBy(e => e.RotationListSortOrder)
                    .ToList();

                // calculate the Yes Count based on the RentalRequestList
                request.YesCount = CalculateYesCount(request);

                // calculate YTD hours for the equipment records
                if (request.HetRentalRequestRotationList != null)
                {
                    foreach (HetRentalRequestRotationList rotationList in request.HetRentalRequestRotationList)
                    {
                        if (rotationList.Equipment != null)
                        {
                            int numberOfBlocks = 0;

                            // get number of blocks for this equipment type
                            if (rotationList.Equipment.DistrictEquipmentType != null)
                            {
                                numberOfBlocks = rotationList.Equipment.DistrictEquipmentType.EquipmentType.IsDumpTruck
                                    ? scoringRules.GetTotalBlocks("DumpTruck") + 1
                                    : scoringRules.GetTotalBlocks() + 1;
                            }

                            // get equipment seniority
                            float seniority = 0F;
                            if (rotationList.Equipment.Seniority != null)
                            {
                                seniority = (float)rotationList.Equipment.Seniority;
                            }

                            // get equipment block number
                            int blockNumber = 0;
                            if (rotationList.Equipment.BlockNumber != null)
                            {
                                blockNumber = (int)rotationList.Equipment.BlockNumber;

                                //HETS-968 - Rotation list -Wrong Block number for Open block
                                if (blockNumber == numberOfBlocks)
                                {
                                    blockNumber = 3;
                                    rotationList.Equipment.BlockNumber = blockNumber;
                                }
                            }

                            rotationList.Equipment.HoursYtd = EquipmentHelper.GetYtdServiceHours(rotationList.Equipment.EquipmentId, context);
                            rotationList.Equipment.SeniorityString = EquipmentHelper.FormatSeniorityString(seniority, blockNumber, numberOfBlocks);
                        }
                    }
                }
            }

            return request;
        }

        #endregion

        #region Convert full equipment record to a "Lite" version

        /// <summary>
        /// Convert to Rental Request Lite Model
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static RentalRequestLite ToLiteModel(HetRentalRequest request)
        {
            RentalRequestLite requestLite = new RentalRequestLite();

            if (request != null)
            {
                requestLite.YesCount = CalculateYesCount(request);

                if (request.DistrictEquipmentType != null)
                {
                    requestLite.EquipmentTypeName = request.DistrictEquipmentType.EquipmentType.Name;
                    requestLite.DistrictEquipmentName = request.DistrictEquipmentType.DistrictEquipmentName;
                }

                requestLite.Id = request.RentalRequestId;
                requestLite.LocalArea = request.LocalArea;

                if (request.Project != null)
                {
                    requestLite.PrimaryContact = request.Project.PrimaryContact;
                    requestLite.ProjectName = request.Project.Name;
                    requestLite.ProjectId = request.Project.ProjectId;
                }
                else
                {
                    requestLite.ProjectName = "Request - View Only";
                }

                requestLite.Status = request.RentalRequestStatusType.Description;
                requestLite.EquipmentCount = request.EquipmentCount;
                requestLite.ExpectedEndDate = request.ExpectedEndDate;
                requestLite.ExpectedStartDate = request.ExpectedStartDate;
            }

            return requestLite;
        }

        #endregion

        #region Convert full equipment record to a "Hires" version

        /// <summary>
        /// Convert to Rental Request Hires (Lite) Model
        /// </summary>
        /// <param name="request"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static RentalRequestHires ToHiresModel(HetRentalRequestRotationList request, HetUser user)
        {
            RentalRequestHires requestLite = new RentalRequestHires();

            if (request != null)
            {
                requestLite.Id = request.RentalRequestRotationListId;
                requestLite.OwnerId = request.Equipment.OwnerId ?? 0;
                requestLite.EquipmentId = request.EquipmentId ?? 0;

                requestLite.LocalAreaName = request.RentalRequest.LocalArea.Name;
                requestLite.ServiceAreaId = request.RentalRequest.LocalArea.ServiceArea.ServiceAreaId;

                // owner data
                requestLite.OwnerCode = request.Equipment.Owner.OwnerCode;
                requestLite.CompanyName = request.Equipment.Owner.OrganizationName;

                // equipment data
                requestLite.EquipmentCode = request.Equipment.EquipmentCode;
                requestLite.EquipmentPrefix = Regex.Match(request.Equipment.EquipmentCode, @"^[^\d-]+").Value;
                requestLite.EquipmentNumber = int.Parse(Regex.Match(request.Equipment.EquipmentCode, @"\d+").Value);
                requestLite.EquipmentMake = request.Equipment.Make;
                requestLite.EquipmentModel = request.Equipment.Model;
                requestLite.EquipmentSize = request.Equipment.Size;
                requestLite.EquipmentYear = request.Equipment.Year;

                // project data
                requestLite.ProjectId = request.RentalRequest.Project.ProjectId;
                requestLite.ProjectNumber = request.RentalRequest.Project.ProvincialProjectNumber;

                requestLite.NoteDate = request.OfferResponseDatetime;

                // Note Type -
                // * Not hired (for recording the response NO for hiring.
                // * Force Hire -For force hiring an equipment
                requestLite.NoteType = "Not Hired"; // default
                requestLite.Reason = request.OfferRefusalReason;
                requestLite.OfferResponseNote = request.OfferResponseNote;

                if (request.IsForceHire != null && request.IsForceHire == true)
                {
                    requestLite.NoteType = "Force Hire";
                    requestLite.Reason = request.Note;
                }

                requestLite.UserId = request.AppCreateUserid;

                if (user != null)
                {
                    requestLite.UserName = user.GivenName ?? "";

                    if (requestLite.UserName.Length > 0)
                    {
                        requestLite.UserName = requestLite.UserName + " ";
                    }

                    requestLite.UserName = requestLite.UserName + user.Surname;
                }
            }

            return requestLite;
        }

        #endregion

        #region Calculate the Number of "Yes" responses to a Rental Request

        /// <summary>
        /// Check how many Yes' we currently have from Owners
        /// </summary>
        /// <returns></returns>
        public static int CalculateYesCount(HetRentalRequest rentalRequest)
        {
            int temp = 0;

            if (rentalRequest.HetRentalRequestRotationList != null)
            {
                foreach (HetRentalRequestRotationList equipment in rentalRequest.HetRentalRequestRotationList)
                {
                    if (equipment.OfferResponse != null &&
                        equipment.OfferResponse.Equals("Yes", StringComparison.InvariantCultureIgnoreCase))
                    {
                        temp++;
                    }

                    if (equipment.IsForceHire != null &&
                        equipment.IsForceHire == true)
                    {
                        temp++;
                    }
                }
            }

            // set the current Yes / Forced Hire Count
            return temp;
        }

        #endregion

        #region Get the number of blocks for the request / equipment type

        /// <summary>
        /// Get the number of blocks for this type of equipment
        /// </summary>
        /// <param name="item"></param>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private static int GetNumberOfBlocks(HetRentalRequest item, DbAppContext context, IConfiguration configuration)
        {
            int numberOfBlocks = -1;

            try
            {
                SeniorityScoringRules scoringRules = new SeniorityScoringRules(configuration);

                // get record
                HetDistrictEquipmentType equipment = context.HetDistrictEquipmentType.AsNoTracking()
                    .Include(x => x.EquipmentType)
                    .FirstOrDefault(x => x.DistrictEquipmentTypeId == item.DistrictEquipmentTypeId);

                if (equipment == null) return 0;

                numberOfBlocks = equipment.EquipmentType.IsDumpTruck ?
                    scoringRules.GetTotalBlocks("DumpTruck") : scoringRules.GetTotalBlocks();
            }
            catch
            {
                // do nothing
            }

            return numberOfBlocks;
        }

        #endregion

        #region Create new Rental Request Rotation List

        /// <summary>
        /// Create Rental Request Rotation List
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public static HetRentalRequest CreateRotationList(HetRentalRequest request, DbAppContext context, IConfiguration configuration)
        {
            var hetRentalRequestRotationList = new List<HetRentalRequestRotationList>();

            request.HetRentalRequestRotationList = hetRentalRequestRotationList;

            // validate input parameters
            if (request.LocalAreaId == null || request.DistrictEquipmentTypeId == null) return request;

            int currentSortOrder = 1;

            // get the number of blocks for this piece of equipment
            int numberOfBlocks = GetNumberOfBlocks(request, context, configuration);
            numberOfBlocks = numberOfBlocks + 1;

            int? statusId = StatusHelper.GetStatusId(HetEquipment.StatusApproved, "equipmentStatus", context);
            if (statusId == null) throw new ArgumentException("Status Id cannot be null");

            // get the equipment based on the current seniority list for the area
            // (and sort the results based on block then
            //      numberInBlock -> ensures consistency with the UI)
            for (int currentBlock = 1; currentBlock <= numberOfBlocks; currentBlock++)
            {
                // start by getting the current set of equipment for the given equipment type
                List<HetEquipment> blockEquipment = context.HetEquipment.AsNoTracking()
                    .Where(x => x.DistrictEquipmentTypeId == request.DistrictEquipmentTypeId &&
                                x.BlockNumber == currentBlock &&
                                x.LocalAreaId == request.LocalAreaId &&
                                x.EquipmentStatusTypeId == statusId)
                    .OrderBy(x => x.NumberInBlock)
                    .ToList();

                int listSize = blockEquipment.Count;

                for (int i = 0; i < listSize; i++)
                {
                    HetRentalRequestRotationList rentalRequestRotationList = new HetRentalRequestRotationList
                    {
                        Equipment = blockEquipment[i],
                        EquipmentId = blockEquipment[i].EquipmentId,
                        SeniorityFloat = blockEquipment[i].Seniority,
                        BlockNumber = blockEquipment[i].BlockNumber,
                        AppCreateTimestamp = DateTime.UtcNow,
                        RotationListSortOrder = currentSortOrder
                    };

                    hetRentalRequestRotationList.Add(rentalRequestRotationList);

                    currentSortOrder++;
                }
            }

            // update the local area rotation list - find record #1
            request = SetupNewRotationList(request, numberOfBlocks, context);

            // remove equipment records
            foreach (HetRentalRequestRotationList rotationList in request.HetRentalRequestRotationList)
            {
                rotationList.Equipment = null;
            }

            // Done!
            return request;
        }

        #endregion

        #region Setup Local Area Rotation Lists

        private static void DropHiredEquipment(List<HetRentalRequestRotationList> hetRentalRequestRotationList, DbAppContext context)
        {
            // check if any items have "Active" contracts - and drop them from the list
            // * ensure we ignore "blank" rental agreements (i.e. rental request is null)

            int? statusIdRentalAgreement = StatusHelper.GetStatusId(HetRentalAgreement.StatusActive, "rentalAgreementStatus", context);
            if (statusIdRentalAgreement == null) throw new ArgumentException("Status Id cannot be null");

            int listSize = hetRentalRequestRotationList.Count;

            for (int i = listSize - 1; i >= 0; i--)
            {
                bool agreementExists = context.HetRentalAgreement.AsNoTracking()
                    .Any(x => x.EquipmentId == hetRentalRequestRotationList[i].EquipmentId &&
                              x.RentalRequestId != null &&
                              x.RentalAgreementStatusTypeId == statusIdRentalAgreement);

                if (agreementExists)
                {
                    hetRentalRequestRotationList.Remove(hetRentalRequestRotationList[i]);
                }
            }
        }

        private static bool IsEquipmentHired(int? equipmentId, DbAppContext context)
        {
            if (equipmentId == null) return false;

            // check if this item has an "Active" contract
            int? statusIdRentalAgreement = StatusHelper.GetStatusId(HetRentalAgreement.StatusActive, "rentalAgreementStatus", context);
            if (statusIdRentalAgreement == null) throw new ArgumentException("Status Id cannot be null");

            bool agreementExists = context.HetRentalAgreement.AsNoTracking()
                .Any(x => x.EquipmentId == equipmentId &&
                          x.RentalRequestId != null &&
                          x.RentalAgreementStatusTypeId == statusIdRentalAgreement);

            return agreementExists;
        }

        private static HetEquipment LastAskedByBlock(int blockNumber, int? districtEquipmentTypeId,
            int? localAreaId, DateTime fiscalStart, DbAppContext context, List<HetRentalRequestRotationList> hetRentalRequestRotationList)
        {
            if (districtEquipmentTypeId == null || localAreaId == null) return null;

            // if this is not block 1 - check that we have "asked" anyone in the previous list
            var rotationListquery = context.HetRentalRequestRotationList.AsNoTracking()
                .Include(x => x.RentalRequest)
                .Include(x => x.Equipment)
                .Where(x => x.RentalRequest.DistrictEquipmentTypeId == districtEquipmentTypeId &&
                            x.RentalRequest.LocalAreaId == localAreaId &&
                            x.RentalRequest.AppCreateTimestamp >= fiscalStart &&
                            x.Equipment.BlockNumber == blockNumber &&
                            x.WasAsked == true &&
                            x.IsForceHire != true)
                .OrderByDescending(x => x.RentalRequestId)
                .ThenByDescending(x => x.RotationListSortOrder);

            foreach(var equipment in rotationListquery)
            {
                if (hetRentalRequestRotationList.Any(x => x.BlockNumber == blockNumber && x.EquipmentId == equipment.EquipmentId))
                    return equipment.Equipment;
            }

            return null;
        }

        /// <summary>
        /// New Rotation List
        /// * Find Record Number 1
        /// * Then update the Local Area Rotation List
        ///
        /// Business rules
        /// * is this the first request of the new fiscal year -> Yes: start from #1
        /// * get the "next equipment to be asked" from "LOCAL_AREA_ROTATION_LIST"
        ///   -> if this is Block 1 -> temporarily becomes #1 on the list
        ///   -> if not block 1 -> #1 i block 1 temporarily becomes #1 on the list
        /// * check all records before temporary #1
        ///   -> if a record was Force Hired -> make them #1
        /// </summary>
        /// <param name="rentalRequest"></param>
        /// <param name="numberOfBlocks"></param>
        /// <param name="context"></param>
        public static HetRentalRequest SetupNewRotationList(HetRentalRequest rentalRequest, int numberOfBlocks, DbAppContext context)
        {
            // remove hired equipment from the list
            DropHiredEquipment((List<HetRentalRequestRotationList>)rentalRequest.HetRentalRequestRotationList, context);

            // nothing to do!
            if (rentalRequest.HetRentalRequestRotationList.Count <= 0) return rentalRequest;

            // sort our new rotation list
            var hetRentalRequestRotationList = rentalRequest.HetRentalRequestRotationList.OrderBy(x => x.RotationListSortOrder).ToList();
            rentalRequest.HetRentalRequestRotationList = hetRentalRequestRotationList;

            int? disEquipmentTypeId = rentalRequest.DistrictEquipmentTypeId;
            int? localAreaId = rentalRequest.LocalAreaId;

            // determine current fiscal year - check for existing rotation lists this year
            // HETS-1195: Adjust seniority list and rotation list for lists hired between Apr1 and roll over
            // ** Need to use the "rollover date" to ensure we don't include records created
            //    after April 1 (but before rollover)
            HetLocalArea localArea = context.HetLocalArea.AsNoTracking()
                .Include(x => x.ServiceArea.District)
                .First(x => x.LocalAreaId == localAreaId);

            HetDistrictStatus districtStatus = context.HetDistrictStatus.AsNoTracking()
                .First(x => x.DistrictId == localArea.ServiceArea.DistrictId);

            DateTime fiscalStart = districtStatus.RolloverEndDate;

            if (fiscalStart == new DateTime(0001, 01, 01, 00, 00, 00))
            {
                int fiscalYear = Convert.ToInt32(districtStatus.NextFiscalYear); // status table uses the start of the year
                fiscalStart = new DateTime(fiscalYear - 1, 4, 1);
            }

            // get the last rotation list created this fiscal year
            bool previousRequestExists = context.HetRentalRequest
                .Any(x => x.DistrictEquipmentType.DistrictEquipmentTypeId == disEquipmentTypeId &&
                          x.LocalArea.LocalAreaId == localAreaId &&
                          x.AppCreateTimestamp >= fiscalStart);

            // *****************************************************************
            // if we don't have a request for the current fiscal,
            // ** pick the first one in the list and we are done.
            // *****************************************************************
            if (!previousRequestExists)
            {
                var firstOnList = hetRentalRequestRotationList[0];
                rentalRequest.FirstOnRotationListId = firstOnList.EquipmentId;

                return rentalRequest; 
            }

            // *****************************************************************
            // use the previous rotation list to determine where we were
            // ** find the equipment after the last "asked in each block
            // ** locate the first equipment and its block number on list in the list
            // *****************************************************************
            int startBlockIndex = -1; //the block index of the first equipment in the new rotation list
            int startBlockNumber = -1;

            (HetEquipment equipment, int position)[] startEquipInBlock = new (HetEquipment, int)[numberOfBlocks];

            // find the equipment after the last asked in each block
            for (int blockIndex = 0; blockIndex < numberOfBlocks; blockIndex++)
            {
                var blockNumber = blockIndex + 1;
                startEquipInBlock[blockIndex].position = -1;

                // get the last asked equipment id for this "block". This method ensures that the returned equipment exists in our list.
                var lastEquipment = LastAskedByBlock(blockNumber, rentalRequest.DistrictEquipmentTypeId, rentalRequest.LocalAreaId,
                    fiscalStart, context, hetRentalRequestRotationList);

                // nothing found for this block - start at 0
                if (lastEquipment == null && hetRentalRequestRotationList.Count > 0)
                {
                    for (int i = 0; i < hetRentalRequestRotationList.Count; i++)
                    {
                        if (hetRentalRequestRotationList[i].BlockNumber != blockNumber) continue;

                        startEquipInBlock[blockIndex].equipment = hetRentalRequestRotationList[i].Equipment;
                        startEquipInBlock[blockIndex].position = i;
                        break;
                    }
                }
                else
                {
                    //we know the equipment exists in the list
                    var foundIndex = hetRentalRequestRotationList.FindIndex(x => x.EquipmentId == lastEquipment.EquipmentId);

                    //find the next record which has the same block
                    for (int i = foundIndex + 1; i < hetRentalRequestRotationList.Count; i++)
                    {
                        if (hetRentalRequestRotationList[i].BlockNumber != blockNumber) continue;

                        startEquipInBlock[blockIndex].equipment = hetRentalRequestRotationList[i].Equipment;
                        startEquipInBlock[blockIndex].position = i;
                        break;
                    }
                }

                //if we haven't found a start equip yet, choose the first one in the block.
                if (startEquipInBlock[blockIndex].equipment == null)
                {
                    var foundIndex = hetRentalRequestRotationList.FindIndex(x => x.BlockNumber == blockNumber);
                    if (foundIndex >= 0)
                    {
                        startEquipInBlock[blockIndex].equipment = hetRentalRequestRotationList[foundIndex].Equipment;
                        startEquipInBlock[blockIndex].position = foundIndex;
                    }
                }
            }

            // find the starting equipment and its block number on the list
            for (int blockIndex = 0; blockIndex < numberOfBlocks; blockIndex++)
            {
                if (startEquipInBlock[blockIndex].equipment != null)
                {
                    startBlockNumber = (int)startEquipInBlock[blockIndex].equipment.BlockNumber;
                    startBlockIndex = startBlockNumber - 1;
                    rentalRequest.FirstOnRotationListId = startEquipInBlock[blockIndex].equipment.EquipmentId;
                    break;
                }
            }

            // *****************************************************************
            // Reset the rotation list sort order
            // *****************************************************************
            int masterSortOrder = 0;

            #region starting block
            for (int i = startEquipInBlock[startBlockIndex].position; i < hetRentalRequestRotationList.Count; i++)
            {
                if (hetRentalRequestRotationList[i].BlockNumber != startBlockNumber)
                    break;

                masterSortOrder++;
                hetRentalRequestRotationList[i].RotationListSortOrder = masterSortOrder;
            }

            // finish the "first set" of records in the block (before the starting position)
            for (int i = 0; i < startEquipInBlock[startBlockIndex].position; i++)
            {
                if (hetRentalRequestRotationList[i].BlockNumber != startBlockNumber)
                    continue;

                masterSortOrder++;
                hetRentalRequestRotationList[i].RotationListSortOrder = masterSortOrder;
            }
            #endregion

            #region remaining blocks if any
            for (int blockIndex = startBlockIndex + 1; blockIndex < numberOfBlocks; blockIndex++)
            {
                var blockNumber = blockIndex + 1;
                for (int i = startEquipInBlock[blockIndex].position; i < hetRentalRequestRotationList.Count; i++)
                {
                    if (i < 0 || hetRentalRequestRotationList[i].BlockNumber != blockNumber)
                        break;

                    masterSortOrder++;
                    hetRentalRequestRotationList[i].RotationListSortOrder = masterSortOrder;
                }

                // finish the "first set" of records in the block (before the starting position)
                for (int i = 0; i < startEquipInBlock[blockIndex].position; i++)
                {
                    if (hetRentalRequestRotationList[i].BlockNumber != blockNumber)
                        continue;

                    masterSortOrder++;
                    hetRentalRequestRotationList[i].RotationListSortOrder = masterSortOrder;
                }
            }
            #endregion

            return rentalRequest;
        }

        #endregion

        /// <summary>
        /// Update the Local Area Rotation List
        /// </summary>
        /// <param name="request"></param>
        /// <param name="numberOfBlocks"></param>
        /// <param name="context"></param>
        public static void UpdateRotationList(HetRentalRequest request)
        {
            if (request.HetRentalRequestRotationList.Count > 0)
            {
                request.HetRentalRequestRotationList = request.HetRentalRequestRotationList
                    .OrderBy(x => x.RotationListSortOrder).ToList();

                request.FirstOnRotationListId = request.HetRentalRequestRotationList.ElementAt(0).Equipment.EquipmentId;
            }
        }

        #region Set Status of new Rental Request

        /// <summary>
        /// Set the Status of the Rental Request Rotation List
        /// </summary>
        /// <param name="rentalRequest"></param>
        /// <param name="context"></param>
        public static string RentalRequestStatus(HetRentalRequest rentalRequest, DbAppContext context)
        {
            string tempStatus = "New";

            // validate input parameters
            if (rentalRequest?.LocalAreaId != null && rentalRequest.DistrictEquipmentTypeId != null)
            {
                int? statusIdInProgress = StatusHelper.GetStatusId(HetRentalRequest.StatusInProgress, "rentalRequestStatus", context);
                if (statusIdInProgress == null) return null;

                // check if there is an existing "In Progress" Rental Request
                List<HetRentalRequest> requests = context.HetRentalRequest
                    .Where(x => x.DistrictEquipmentType.DistrictEquipmentTypeId == rentalRequest.DistrictEquipmentTypeId &&
                                x.LocalArea.LocalAreaId == rentalRequest.LocalAreaId &&
                                x.RentalRequestStatusTypeId == statusIdInProgress)
                    .ToList();

                tempStatus = requests.Count == 0 ? "In Progress" : "New";
            }

            return tempStatus;
        }

        #endregion

        #region Get Rental Request History

        public static List<History> GetHistoryRecords(int id, int? offset, int? limit, DbAppContext context)
        {
            HetRentalRequest request = context.HetRentalRequest.AsNoTracking()
                .Include(x => x.HetHistory)
                .First(a => a.RentalRequestId == id);

            List<HetHistory> data = request.HetHistory
                .OrderByDescending(y => y.AppLastUpdateTimestamp)
                .ToList();

            if (offset == null)
            {
                offset = 0;
            }

            if (limit == null)
            {
                limit = data.Count - offset;
            }

            List<History> result = new List<History>();

            for (int i = (int)offset; i < data.Count && i < offset + limit; i++)
            {
                History temp = new History();

                if (data[i] != null)
                {
                    temp.HistoryText = data[i].HistoryText;
                    temp.Id = data[i].HistoryId;
                    temp.LastUpdateTimestamp = data[i].AppLastUpdateTimestamp;
                    temp.LastUpdateUserid = data[i].AppLastUpdateUserid;
                    temp.AffectedEntityId = data[i].RentalRequestId;
                }

                result.Add(temp);
            }

            return result;
        }

        #endregion
    }
}
