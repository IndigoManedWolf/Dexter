﻿using Dexter.Abstractions;
using System;

namespace Dexter.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireModeratorAttribute : RequirePermissionLevelAttribute {
        public RequireModeratorAttribute() : base(PermissionLevel.Moderator) { }
    }
}
