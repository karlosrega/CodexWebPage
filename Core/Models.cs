using System;
using System.Collections.Generic;

namespace A1IntegrationsAdmin.Core
{
    /// <summary>Authenticated identity. Permissions are reloaded from the database on every request.</summary>
    public sealed class CurrentUser
    {
        public int Id
        {
            get; set;
        }
        public string UserName
        {
            get; set;
        }
        public string DisplayName
        {
            get; set;
        }
        public HashSet<string> Permissions { get; } = new HashSet<string>(StringComparer.Ordinal);
        public bool Can(string permission) => Permissions.Contains(permission);
    }

    public sealed class UserRecord
    {
        public int Id
        {
            get; set;
        }
        public string UserName
        {
            get; set;
        }
        public string DisplayName
        {
            get; set;
        }
        public string Email
        {
            get; set;
        }
        public bool Active
        {
            get; set;
        }
        public int[] ProfileIds { get; set; } = new int[0];
        public string Profiles
        {
            get; set;
        }
    }

    public sealed class ProfileRecord
    {
        public int Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }
        public string Description
        {
            get; set;
        }
        public bool Active
        {
            get; set;
        }
        public bool System
        {
            get; set;
        }
        public string[] Permissions { get; set; } = new string[0];
    }

    public sealed class Promotion
    {
        public int Id
        {
            get; set;
        }
        public int Position
        {
            get; set;
        }
        public string Anchor
        {
            get; set;
        }
        public string Category
        {
            get; set;
        }
        public string Title
        {
            get; set;
        }
        public string Body
        {
            get; set;
        }
        public string Image
        {
            get; set;
        }
        public string Link
        {
            get; set;
        }
    }

    public sealed class PageResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Total
        {
            get; set;
        }
        public int Page
        {
            get; set;
        }
        public int Pages => Math.Max(1, (Total + 11) / 12);
    }

    public sealed class RuleException : Exception
    {
        public RuleException(string message) : base(message) { }
    }
}
