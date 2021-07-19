﻿using System;
using System.Collections.Generic;

#nullable disable

namespace HetsData.Model
{
    public partial class HetRolloverProgress
    {
        public int DistrictId { get; set; }
        public int? ProgressPercentage { get; set; }

        public virtual HetDistrict District { get; set; }
    }
}
