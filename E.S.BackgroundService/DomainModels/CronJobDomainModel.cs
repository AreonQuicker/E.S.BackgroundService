using E.S.Data.Query.Context.Attributes;
using E.S.Data.Query.Context.Enums;

namespace E.S.BackgroundService.DomainModels
{
    [DataQueryContext("CronJob", "BackgroundService")]
    public class CronJobDomainModel
    {
        [DataQueryContextIdProperty]
        [DataQueryContextProperty(DataQueryContextPropertyFlags.None)]
        public int Id { get; init; }

        public DateTime CreatedDate { get; init; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string Job { get; set; }
    }
}