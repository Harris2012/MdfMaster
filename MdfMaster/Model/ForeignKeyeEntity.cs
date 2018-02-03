using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MdfMaster.Model
{
    public class ForeignKeyeEntity
    {
        /// <summary>
        /// 外键名称
        /// </summary>
        public string FkName { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        public string ForeignTable { get; set; }

        /// <summary>
        /// 列名
        /// </summary>
        public string ForeignColumn { get; set; }

        /// <summary>
        /// 主表名称
        /// </summary>
        public string MainTable { get; set; }

        /// <summary>
        /// 主表
        /// </summary>
        public string MainColumn { get; set; }
    }
}
