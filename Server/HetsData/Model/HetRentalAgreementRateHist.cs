﻿using System;
using Newtonsoft.Json;

namespace HetsData.Model
{
    public partial class HetRentalAgreementRateHist
    {
        [JsonProperty("Id")]
        public int RentalAgreementRateHistId { get; set; }

        public int RentalAgreementRateId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Comment { get; set; }
        public string ComponentName { get; set; }
        public float? Rate { get; set; }
        public int? RatePeriodTypeId { get; set; }
        public bool Overtime { get; set; }
        public bool Active { get; set; }
        public bool IsIncludedInTotal { get; set; }
        public int? RentalAgreementId { get; set; }
        [JsonIgnore]public string AppCreateUserDirectory { get; set; }
        [JsonIgnore]public string AppCreateUserGuid { get; set; }
        [JsonIgnore]public string AppCreateUserid { get; set; }
        [JsonIgnore]public DateTime AppCreateTimestamp { get; set; }
        [JsonIgnore]public string AppLastUpdateUserDirectory { get; set; }
        [JsonIgnore]public string AppLastUpdateUserGuid { get; set; }
        [JsonIgnore]public string AppLastUpdateUserid { get; set; }
        [JsonIgnore]public DateTime AppLastUpdateTimestamp { get; set; }
        [JsonIgnore]public string DbCreateUserId { get; set; }
        [JsonIgnore]public DateTime DbCreateTimestamp { get; set; }
        [JsonIgnore]public DateTime DbLastUpdateTimestamp { get; set; }
        [JsonIgnore]public string DbLastUpdateUserId { get; set; }
        public int ConcurrencyControlNumber { get; set; }
    }
}
