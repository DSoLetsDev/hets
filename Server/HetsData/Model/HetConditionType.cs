﻿using System;
using System.Collections.Generic;

#nullable disable

namespace HetsData.Model
{
    public partial class HetConditionType
    {
        public int ConditionTypeId { get; set; }
        public int? DistrictId { get; set; }
        public string ConditionTypeCode { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public string AppCreateUserDirectory { get; set; }
        public string AppCreateUserGuid { get; set; }
        public string AppCreateUserid { get; set; }
        public DateTime AppCreateTimestamp { get; set; }
        public string AppLastUpdateUserDirectory { get; set; }
        public string AppLastUpdateUserGuid { get; set; }
        public string AppLastUpdateUserid { get; set; }
        public DateTime AppLastUpdateTimestamp { get; set; }
        public string DbCreateUserId { get; set; }
        public DateTime DbCreateTimestamp { get; set; }
        public DateTime DbLastUpdateTimestamp { get; set; }
        public string DbLastUpdateUserId { get; set; }
        public int ConcurrencyControlNumber { get; set; }

        public virtual HetDistrict District { get; set; }
    }
}
