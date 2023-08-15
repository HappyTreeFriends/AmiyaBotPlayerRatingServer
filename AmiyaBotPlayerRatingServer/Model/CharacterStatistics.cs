namespace AmiyaBotPlayerRatingServer.Model
{
    public class CharacterStatistics
    {
        public String Id { get; set; }

        /// <summary>
        /// 该统计数据的起止时间(起)
        /// </summary>
        public DateTime VersionStart { get; set; }
        /// <summary>
        /// 该统计数据的起止时间(止)
        /// </summary>
        public DateTime VersionEnd { get; set; }

        /// <summary>
        /// 该统计数据的样本总数
        /// </summary>
        public long SampleCount { get; set; }

        /// <summary>
        /// 平均精英化等级
        /// </summary>
        public int AverageExptLevel { get; set; }
        /// <summary>
        /// 平均等级
        /// </summary>
        public int AverageLevel { get; set; }


    }
}
