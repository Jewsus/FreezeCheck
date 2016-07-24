using System;
using System.ComponentModel;

namespace FreezeCheck
{
    public static class Permissions
    {
        [Description("User can freezecheck")]
        public static readonly string cancheck = "freezecheck.check";

        [Description("User can freezecheck others")]
        public static readonly string cancheckothers = "freezecheck.admin";

        [Description("User can enforce unfreeze")]
        public static readonly string canforce = "freezecheck.force";
    }
}
