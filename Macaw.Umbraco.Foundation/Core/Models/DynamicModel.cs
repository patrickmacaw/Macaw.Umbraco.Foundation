﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Macaw.Umbraco.Foundation.Core.Models
{
	/// <summary>
	/// DynamicModel is using Umbraco's standard DynamicPublishedContent
	/// https://github.com/MacawNL/Macaw.Umbraco.Foundation/wiki/Core-Models
	/// Cause we like to have some extra control over some properties we implement our own IPublishedContent as the base class
	/// Instead of using DynamicPublishedContent directly.
	/// </summary>
	public class DynamicModel : DynamicObject, IPublishedContent, INullModel
	{
        public UmbracoHelper Helper = new UmbracoHelper(UmbracoContext.Current);
        protected DynamicPublishedContent Source;

        /// <summary>
        /// Dynamic representaion of the source / current page...
        /// </summary>
        public virtual dynamic Dynamic
        {
            get
            {
                return Source;
            }
        }

        public ISiteRepository Repository { get; protected set; }

        public DynamicModel(IPublishedContent source, ISiteRepository repository)
        {
            if (source is DynamicPublishedContent)
            {
                Source = (DynamicPublishedContent)source;
            }
            else if (source is DynamicModel)
            {
                Source = ((DynamicModel)source).Source;
            }
            else
            {
                Source = source.AsDynamic() as DynamicPublishedContent;
            }

            Repository = repository;
        }

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			var ret = Source.TryGetMember(binder, out result);
			return ret;
		}

        private DynamicModel homePage;

		public virtual DynamicModel Homepage
		{
			get
			{
                if(homePage == null)
                {
                    homePage = Repository.GetHomePage();
                }
                return homePage;
			}
		}

		public virtual DateTime PublishDate //late binding of the publishdate...
		{
			get
			{
				var content = Repository.FindContentById(this.Id);
				var ret = content.ReleaseDate;

				if (ret == null)
				{
					return this.UpdateDate;
				}
				else
				{
					return ret.Value;
				}
			}
		}

		/// <summary>
		/// Convert this item to a custom type that inherits from dynamic model
		/// The class must have a 2 parameter constructor.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T As<T>() where T : DynamicModel
		{
			if (this is T)
				return this as T;
			else
				return (T)Activator.CreateInstance(typeof(T), this, Repository);
		}

		IPublishedContent IPublishedContent.Parent
		{
			get
			{
				return Source.Parent;
			}
		}

		public virtual DynamicModel Parent
		{
			get
			{
				return new DynamicModel(Source.Parent, Repository);
			}
		}

		#region children

		public bool HasChildren(bool includeHiddenItems)
		{
			if (includeHiddenItems)
				return Children.Count() > 0;
			else
				return Children.Where(c => c.IsVisible()).Count() > 0;
		}

		public bool HasChildren()
		{
			return HasChildren(true);
		}

		/// <summary>
		/// return all children as a "hybrid" model that inherits from Dynamic model
		/// and has the same 2 parameter constructor.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IEnumerable<T> ChildrenAs<T>(bool includeHiddenItems) where T : DynamicModel
		{
			foreach (var child in Source.Children)
				if (includeHiddenItems || child.IsVisible())
					yield return (T)Activator.CreateInstance(typeof(T), child, Repository);
		}

		/// <summary>
		/// default, all children are returned.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IEnumerable<T> ChildrenAs<T>() where T : DynamicModel
		{
			return ChildrenAs<T>(true);
		}

		public virtual IEnumerable<DynamicModel> Children
		{
			get
			{
				return ChildrenAs<DynamicModel>();
			}
		}

		IEnumerable<IPublishedContent> IPublishedContent.Children
		{
			get { return Source.Children; }
		}

		public virtual IEnumerable<DynamicModel> Breadcrumbs
		{
			get
			{
				return BreadcrumbsAs<DynamicModel>();
			}
		}

		/// <summary>
		/// returns Ancestors (in the order of the lowest level first)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="includeHiddenItems"></param>
		/// <returns></returns>
		public IEnumerable<T> BreadcrumbsAs<T>(bool includeHiddenItems) where T : DynamicModel
		{
			foreach (var anch in Source.Ancestors().OrderBy("level"))
			{
				if (includeHiddenItems || anch.IsVisible())
					yield return (T)Activator.CreateInstance(typeof(T), anch, Repository);
			}
		}

		/// <summary>
		/// returns Ancestors (in the order of the lowest level first)
		/// default returns hiddenitems are included
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IEnumerable<T> BreadcrumbsAs<T>() where T : DynamicModel
		{
			return BreadcrumbsAs<T>(true);
		}

		#endregion

		public virtual bool IsNull()
		{
			return false;
		}

		public virtual IEnumerable<IPublishedContent> ContentSet
		{
			get { return Source.ContentSet; }
		}

		public virtual global::Umbraco.Core.Models.PublishedContent.PublishedContentType ContentType
		{
			get { return Source.ContentType; }
		}

		public virtual DateTime CreateDate
		{
			get { return Source.CreateDate; }
		}

		public virtual int CreatorId
		{
			get { return Source.CreatorId; }
		}

		public virtual string CreatorName
		{
			get { return Source.CreatorName; }
		}

		public virtual string DocumentTypeAlias
		{
			get { return Source.DocumentTypeAlias; }
		}

		int IPublishedContent.DocumentTypeId
		{
			get { return ((IPublishedContent)Source).DocumentTypeId; }
		}

		public virtual int GetIndex()
		{
			return ((IPublishedContent)Source).GetIndex();
		}

		IPublishedProperty IPublishedContent.GetProperty(string alias, bool recurse)
		{
			return Source.GetProperty(alias, recurse);
		}

		IPublishedProperty IPublishedContent.GetProperty(string alias)
		{
			return Source.GetProperty(alias);
		}

		public virtual int Id
		{
			get { return Source.Id; }
		}

		bool IPublishedContent.IsDraft //todo test this implementation
		{
			get { return ((IPublishedContent)Source).IsDraft; }
		}

		public virtual PublishedItemType ItemType
		{
			get { return Source.ItemType; }
		}

		public virtual int Level
		{
			get { return Source.Level; }
		}

		public virtual string Name
		{
			get { return Source.Name; }
		}

		public virtual string Path
		{
			get { return Source.Path; }
		}

		ICollection<IPublishedProperty> IPublishedContent.Properties //todo: test this implementation
		{
			get { return ((IPublishedContent)Source).Properties; }
		}

		public virtual int SortOrder
		{
			get { return Source.SortOrder; }
		}

		public virtual int TemplateId
		{
			get { return Source.TemplateId; }
		}

		public virtual DateTime UpdateDate
		{
			get { return Source.UpdateDate; }
		}

		public virtual string Url
		{
			get { return Repository.FriendlyUrl(Source); }
		}

		public virtual string UrlName
		{
			get { return Source.UrlName; }
		}

		public virtual Guid Version
		{
			get { return Source.Version; }
		}

		public virtual int WriterId
		{
			get { return Source.WriterId; }
		}

		public virtual string WriterName
		{
			get { return Source.WriterName; }
		}

		public virtual object this[string alias]
		{
			get { return Source[alias]; }
		}

		public virtual bool IsVisible()
		{
			return Source.IsVisible();
		}
	}
}
