using Dashboards_reports.Models.MaterialPlanning.DTO;

namespace Dashboards_reports.Service
{
    public class MRPService
    {
        //public List<MRPRunResultDto> Run(
        //    decimal onHandQty,
        //    decimal safetyStock,
        //    decimal moq,
        //    int leadTimeWeeks,
        //    List<MRPWeekInputDto> weeks)
        //{
        //    var result = new List<MRPRunResultDto>();
        //    decimal projectedAvailable = onHandQty;

        //    foreach (var week in weeks.OrderBy(x => x.WeekNo))
        //    {
        //        decimal netReq = 0;
        //        decimal plannedReceipt = 0;

        //        if ((projectedAvailable - week.GrossRequirements) < safetyStock)
        //        {
        //            netReq =
        //                (safetyStock + week.GrossRequirements) - projectedAvailable;

        //            plannedReceipt =
        //                Math.Ceiling(netReq / moq) * moq;
        //        }

        //        projectedAvailable =
        //            projectedAvailable
        //            + week.ScheduledReceipts
        //            + plannedReceipt
        //            - week.GrossRequirements;

        //        result.Add(new MRPRunResultDto
        //        {
        //            WeekNo = week.WeekNo,
        //            GrossRequirements = week.GrossRequirements,
        //            ScheduledReceipts = week.ScheduledReceipts,
        //            ProjectedAvailableBalance = projectedAvailable,
        //            NetRequirements = netReq,
        //            PlannedOrderReceipt = plannedReceipt,
        //            PlannedOrderRelease = 0
        //        });
        //    }

        //    // Lead time offset
        //    foreach (var row in result)
        //    {
        //        if (row.PlannedOrderReceipt > 0)
        //        {
        //            int releaseWeek = row.WeekNo - leadTimeWeeks;
        //            if (releaseWeek < 0) releaseWeek = 0;

        //            result.First(x => x.WeekNo == releaseWeek)
        //                  .PlannedOrderRelease += row.PlannedOrderReceipt;
        //        }
        //    }

        //    return result;
        //}
        public List<MRPRunResultDto> Run(
            decimal onHandQty,
            decimal safetyStock,
            decimal moq,
            int leadTime,
            List<MRPWeekInputDto> weeks)
        {
            var results = new List<MRPRunResultDto>();

            decimal previousPab = onHandQty; // B8

            for (int i = 0; i < weeks.Count; i++)
            {
                var week = weeks[i];

                // 🔹 NET REQUIREMENTS
                decimal netRequirement =
                    week.GrossRequirements + safetyStock - previousPab;

                if (netRequirement < 0)
                    netRequirement = 0;

                // 🔹 PLANNED ORDER RECEIPT
                decimal plannedReceipt = 0;
                if (netRequirement > 0)
                {
                    plannedReceipt =
                        Math.Ceiling(netRequirement / moq) * moq;
                }

                // 🔹 PLANNED ORDER RELEASE (offset by LT)
                decimal plannedRelease = 0;
                if (plannedReceipt > 0 && i - leadTime >= 0)
                {
                    plannedRelease = plannedReceipt;
                }

                // 🔹 PROJECTED AVAILABLE BALANCE
                decimal pab =
                    previousPab
                    + plannedReceipt
                    - week.GrossRequirements;

                results.Add(new MRPRunResultDto
                {
                    WeekNo = week.WeekNo,
                    GrossRequirements = week.GrossRequirements,

                    ProjectedAvailableBalance = pab,
                    NetRequirements = netRequirement,

                    PlannedOrderReceipt = plannedReceipt,
                    PlannedOrderRelease = plannedRelease
                });

                previousPab = pab; // move to next week
            }

            return results;
        }


    }

}
