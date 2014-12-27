﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

[assembly: EdmSchemaAttribute()]
namespace CommunityBridge3
{
    #region Contexts
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    public partial class NewsgroupsEntities : ObjectContext
    {
        #region Constructors
    
        /// <summary>
        /// Initializes a new NewsgroupsEntities object using the connection string found in the 'NewsgroupsEntities' section of the application configuration file.
        /// </summary>
        public NewsgroupsEntities() : base("name=NewsgroupsEntities", "NewsgroupsEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new NewsgroupsEntities object.
        /// </summary>
        public NewsgroupsEntities(string connectionString) : base(connectionString, "NewsgroupsEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new NewsgroupsEntities object.
        /// </summary>
        public NewsgroupsEntities(EntityConnection connection) : base(connection, "NewsgroupsEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        #endregion
    
        #region Partial Methods
    
        partial void OnContextCreated();
    
        #endregion
    
        #region ObjectSet Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        public ObjectSet<Mapping> Mappings
        {
            get
            {
                if ((_Mappings == null))
                {
                    _Mappings = base.CreateObjectSet<Mapping>("Mappings");
                }
                return _Mappings;
            }
        }
        private ObjectSet<Mapping> _Mappings;
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        public ObjectSet<Version> Versions
        {
            get
            {
                if ((_Versions == null))
                {
                    _Versions = base.CreateObjectSet<Version>("Versions");
                }
                return _Versions;
            }
        }
        private ObjectSet<Version> _Versions;

        #endregion

        #region AddTo Methods
    
        /// <summary>
        /// Deprecated Method for adding a new object to the Mappings EntitySet. Consider using the .Add method of the associated ObjectSet&lt;T&gt; property instead.
        /// </summary>
        public void AddToMappings(Mapping mapping)
        {
            base.AddObject("Mappings", mapping);
        }
    
        /// <summary>
        /// Deprecated Method for adding a new object to the Versions EntitySet. Consider using the .Add method of the associated ObjectSet&lt;T&gt; property instead.
        /// </summary>
        public void AddToVersions(Version version)
        {
            base.AddObject("Versions", version);
        }

        #endregion

    }

    #endregion

    #region Entities
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    [EdmEntityTypeAttribute(NamespaceName="NewsgroupsModel", Name="Mapping")]
    [Serializable()]
    [DataContractAttribute(IsReference=true)]
    public partial class Mapping : EntityObject
    {
        #region Factory Method
    
        /// <summary>
        /// Create a new Mapping object.
        /// </summary>
        /// <param name="postId">Initial value of the PostId property.</param>
        /// <param name="nNTPMessageNumber">Initial value of the NNTPMessageNumber property.</param>
        /// <param name="id">Initial value of the Id property.</param>
        /// <param name="postType">Initial value of the PostType property.</param>
        /// <param name="isPrimary">Initial value of the IsPrimary property.</param>
        public static Mapping CreateMapping(global::System.Guid postId, global::System.Int64 nNTPMessageNumber, global::System.Guid id, global::System.Int32 postType, global::System.Boolean isPrimary)
        {
            Mapping mapping = new Mapping();
            mapping.PostId = postId;
            mapping.NNTPMessageNumber = nNTPMessageNumber;
            mapping.Id = id;
            mapping.PostType = postType;
            mapping.IsPrimary = isPrimary;
            return mapping;
        }

        #endregion

        #region Primitive Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Guid PostId
        {
            get
            {
                return _PostId;
            }
            set
            {
                OnPostIdChanging(value);
                ReportPropertyChanging("PostId");
                _PostId = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("PostId");
                OnPostIdChanged();
            }
        }
        private global::System.Guid _PostId;
        partial void OnPostIdChanging(global::System.Guid value);
        partial void OnPostIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Int64 NNTPMessageNumber
        {
            get
            {
                return _NNTPMessageNumber;
            }
            set
            {
                OnNNTPMessageNumberChanging(value);
                ReportPropertyChanging("NNTPMessageNumber");
                _NNTPMessageNumber = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("NNTPMessageNumber");
                OnNNTPMessageNumberChanged();
            }
        }
        private global::System.Int64 _NNTPMessageNumber;
        partial void OnNNTPMessageNumberChanging(global::System.Int64 value);
        partial void OnNNTPMessageNumberChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=true, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Guid Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (_Id != value)
                {
                    OnIdChanging(value);
                    ReportPropertyChanging("Id");
                    _Id = StructuralObject.SetValidValue(value);
                    ReportPropertyChanged("Id");
                    OnIdChanged();
                }
            }
        }
        private global::System.Guid _Id;
        partial void OnIdChanging(global::System.Guid value);
        partial void OnIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.DateTime> CreatedDate
        {
            get
            {
                return _CreatedDate;
            }
            set
            {
                OnCreatedDateChanging(value);
                ReportPropertyChanging("CreatedDate");
                _CreatedDate = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("CreatedDate");
                OnCreatedDateChanged();
            }
        }
        private Nullable<global::System.DateTime> _CreatedDate;
        partial void OnCreatedDateChanging(Nullable<global::System.DateTime> value);
        partial void OnCreatedDateChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Int32 PostType
        {
            get
            {
                return _PostType;
            }
            set
            {
                OnPostTypeChanging(value);
                ReportPropertyChanging("PostType");
                _PostType = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("PostType");
                OnPostTypeChanged();
            }
        }
        private global::System.Int32 _PostType;
        partial void OnPostTypeChanging(global::System.Int32 value);
        partial void OnPostTypeChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.Guid> ParentId
        {
            get
            {
                return _ParentId;
            }
            set
            {
                OnParentIdChanging(value);
                ReportPropertyChanging("ParentId");
                _ParentId = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("ParentId");
                OnParentIdChanged();
            }
        }
        private Nullable<global::System.Guid> _ParentId;
        partial void OnParentIdChanging(Nullable<global::System.Guid> value);
        partial void OnParentIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.DateTime> LastActivityDate
        {
            get
            {
                return _LastActivityDate;
            }
            set
            {
                OnLastActivityDateChanging(value);
                ReportPropertyChanging("LastActivityDate");
                _LastActivityDate = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("LastActivityDate");
                OnLastActivityDateChanged();
            }
        }
        private Nullable<global::System.DateTime> _LastActivityDate;
        partial void OnLastActivityDateChanging(Nullable<global::System.DateTime> value);
        partial void OnLastActivityDateChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public global::System.String Title
        {
            get
            {
                return _Title;
            }
            set
            {
                OnTitleChanging(value);
                ReportPropertyChanging("Title");
                _Title = StructuralObject.SetValidValue(value, true);
                ReportPropertyChanged("Title");
                OnTitleChanged();
            }
        }
        private global::System.String _Title;
        partial void OnTitleChanging(global::System.String value);
        partial void OnTitleChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.Guid> ParentPostId
        {
            get
            {
                return _ParentPostId;
            }
            set
            {
                OnParentPostIdChanging(value);
                ReportPropertyChanging("ParentPostId");
                _ParentPostId = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("ParentPostId");
                OnParentPostIdChanged();
            }
        }
        private Nullable<global::System.Guid> _ParentPostId;
        partial void OnParentPostIdChanging(Nullable<global::System.Guid> value);
        partial void OnParentPostIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public Nullable<global::System.DateTime> ParentCreatedDate
        {
            get
            {
                return _ParentCreatedDate;
            }
            set
            {
                OnParentCreatedDateChanging(value);
                ReportPropertyChanging("ParentCreatedDate");
                _ParentCreatedDate = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("ParentCreatedDate");
                OnParentCreatedDateChanged();
            }
        }
        private Nullable<global::System.DateTime> _ParentCreatedDate;
        partial void OnParentCreatedDateChanging(Nullable<global::System.DateTime> value);
        partial void OnParentCreatedDateChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=true)]
        [DataMemberAttribute()]
        public global::System.String Info
        {
            get
            {
                return _Info;
            }
            set
            {
                OnInfoChanging(value);
                ReportPropertyChanging("Info");
                _Info = StructuralObject.SetValidValue(value, true);
                ReportPropertyChanged("Info");
                OnInfoChanged();
            }
        }
        private global::System.String _Info;
        partial void OnInfoChanging(global::System.String value);
        partial void OnInfoChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Boolean IsPrimary
        {
            get
            {
                return _IsPrimary;
            }
            set
            {
                OnIsPrimaryChanging(value);
                ReportPropertyChanging("IsPrimary");
                _IsPrimary = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("IsPrimary");
                OnIsPrimaryChanged();
            }
        }
        private global::System.Boolean _IsPrimary;
        partial void OnIsPrimaryChanging(global::System.Boolean value);
        partial void OnIsPrimaryChanged();

        #endregion

    
    }
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    [EdmEntityTypeAttribute(NamespaceName="NewsgroupsModel", Name="Version")]
    [Serializable()]
    [DataContractAttribute(IsReference=true)]
    public partial class Version : EntityObject
    {
        #region Factory Method
    
        /// <summary>
        /// Create a new Version object.
        /// </summary>
        /// <param name="id">Initial value of the Id property.</param>
        /// <param name="version1">Initial value of the Version1 property.</param>
        public static Version CreateVersion(global::System.Guid id, global::System.Int64 version1)
        {
            Version version = new Version();
            version.Id = id;
            version.Version1 = version1;
            return version;
        }

        #endregion

        #region Primitive Properties
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=true, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Guid Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (_Id != value)
                {
                    OnIdChanging(value);
                    ReportPropertyChanging("Id");
                    _Id = StructuralObject.SetValidValue(value);
                    ReportPropertyChanged("Id");
                    OnIdChanged();
                }
            }
        }
        private global::System.Guid _Id;
        partial void OnIdChanging(global::System.Guid value);
        partial void OnIdChanged();
    
        /// <summary>
        /// No Metadata Documentation available.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Int64 Version1
        {
            get
            {
                return _Version1;
            }
            set
            {
                OnVersion1Changing(value);
                ReportPropertyChanging("Version1");
                _Version1 = StructuralObject.SetValidValue(value);
                ReportPropertyChanged("Version1");
                OnVersion1Changed();
            }
        }
        private global::System.Int64 _Version1;
        partial void OnVersion1Changing(global::System.Int64 value);
        partial void OnVersion1Changed();

        #endregion

    
    }

    #endregion

    
}