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
        /// 该批次的总数，和样本总数的区别是，SampleCount不计算未拥有该干员的人的干员练度
        /// </summary>
        public long BatchCount { get; set; }

        /// <summary>
        /// 干员Id
        /// </summary>
        public string CharacterId { get; set; }

        /// <summary>
        /// 角色稀有度,用于帮助计算
        /// </summary>
        public int Rarity { get; set; }

        /// <summary>
        /// 平均精英化等级
        /// </summary>
        public double AverageEvolvePhase { get; set; }

        /// <summary>
        /// 平均等级
        /// </summary>
        public double AverageLevel { get; set; }
        
        /// <summary>
        /// 平均技能等级,数值为1-7
        /// </summary>
        public double AverageSkillLevel { get; set; }

        /// <summary>
        /// 平均专精等级0-3
        /// </summary>
        public List<double> AverageSpecializeLevel { get; set; } = new List<double>();

        /// <summary>
        /// 平均模组等级 0-3
        /// </summary>
        public Dictionary<int, double> AverageEquipLevel { get; set; } =new Dictionary<int, double>();
    }
}
