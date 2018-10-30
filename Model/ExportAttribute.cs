using System;

namespace Model
{
    /// <summary>
    /// 接口的导出特性，具有此特性的接口的实现和枚举类将在接口导入时发现
    /// </summary>
    public class ExportAttribute : Attribute
    {
    }
}