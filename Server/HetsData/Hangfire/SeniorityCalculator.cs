﻿using Hangfire;
using HetsData.Helpers;
using HetsData.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HetsData.Hangfire
{
    public class SeniorityCalculator
    {
        private DbAppContext _dbContext;
        private string _jobId;

        public SeniorityCalculator(DbAppContext dbContext)
        {
            _dbContext = dbContext;
            _jobId = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// Recalculates seniority with the new sorting rule (sorting by equipment code) for the district equipment types that have the same seniority and received date
        /// </summary>
        /// <param name="context"></param>
        /// <param name="seniorityScoringRules"></param>
        /// <param name="connectionString"></param>
        [SkipSameJob]
        [AutomaticRetry(Attempts = 0)]
        public void RecalculateSeniorityList(string seniorityScoringRules)
        {
            // get equipment status
            int? equipmentStatusId = StatusHelper.GetStatusId(HetEquipment.StatusApproved, "equipmentStatus", _dbContext);
            if (equipmentStatusId == null)
            {
                throw new ArgumentException("Status Code not found");
            }

            WriteLog("Recalculation Started");

            var equipments = _dbContext.HetEquipment
                .Where(x => x.EquipmentStatusTypeId == equipmentStatusId)
                .GroupBy(x => new { x.LocalAreaId, x.DistrictEquipmentTypeId, x.Seniority, x.ReceivedDate })
                .Where(x => x.Count() > 1)
                .Select(x => new { x.Key.LocalAreaId, x.Key.DistrictEquipmentTypeId })
                .Distinct()
                .ToList();

            var count = 0;
            foreach (var equipment in equipments)
            {
                EquipmentHelper.RecalculateSeniority(equipment.LocalAreaId, equipment.DistrictEquipmentTypeId, _dbContext, seniorityScoringRules);
                WriteLog($"Processed {count} / {equipments.Count}");
            }

            _dbContext.SaveChanges();

            WriteLog("Recalculation Finished");
        }

        private void WriteLog(string message)
        {
            Console.WriteLine($"Seniority Calculator[{_jobId}] {message}");
        }
    }
}
