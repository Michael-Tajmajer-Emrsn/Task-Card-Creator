﻿// This source is subject to the MIT License.
// Please see https://github.com/frederiksen/Task-Card-Creator for details.
// All other rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using ReportInterface;
using TaskServerServiceInterface;

namespace TFSQueryServices
{
  public class TfsProject : ITaskProject
  {
    internal ProjectInfo projInfo;
    internal WorkItemStore workItemStoreService;
    internal WorkItemTypeCollection wiTypes;
    internal TfsUserControl uc;

    public UserControl CreateUserControl(IEnumerable<IReport> supportedReports, IEnumerable<IReport> allReports)
    {
      uc = new TfsUserControl(supportedReports, allReports);
      uc.BuildQueryTree(workItemStoreService.Projects[projInfo.Name].QueryHierarchy, projInfo.Name);
      uc.workItemStoreService = workItemStoreService;
      return uc;
    }

    public IReport SelectedReport { get { return uc.SelectedReport; } }

    public IEnumerable<string> WorkItemTypeCollection
    {
      get
      {
        var list = new List<string>();
        foreach (WorkItemType wiType in wiTypes)
        {
          list.Add(wiType.Name);
        }
        return list;
      }
    }

    public List<ReportItem> WorkItems
    {
      get
      {
        var l = new List<ReportItem>();
        foreach (var w in uc.SelectedWorkItems)
        {
          var ri = new ReportItem { Id = w.Id.ToString(), Title = w.Title, Type = w.Type.Name, State = w.State, Description = GetDescription(w) };
          foreach (var relatedLink in w.Links.OfType<RelatedLink>().Where(relatedLink => relatedLink.LinkTypeEnd.Name == "Parent"))
          {
            ri.ParentId = relatedLink.RelatedWorkItemId.ToString();
            break;
          }

			foreach (var relatedLink in w.Links.OfType<RelatedLink>().Where(relatedLink => relatedLink.LinkTypeEnd.Name == "Predecessor"))
	        {
		        if (!ri.PredecessorIds.IsNullOrEmpty())
		        {
			        ri.PredecessorIds += ", ";
		        }
		        ri.PredecessorIds = ri.PredecessorIds + relatedLink.RelatedWorkItemId.ToString();
	        }

	        foreach (var relatedLink in w.Links.OfType<RelatedLink>().Where(relatedLink => relatedLink.LinkTypeEnd.Name == "Successor"))
	        {
		        if (!ri.SuccessorIds.IsNullOrEmpty())
		        {
			        ri.SuccessorIds += ", ";
		        }
				ri.SuccessorIds = ri.SuccessorIds + relatedLink.RelatedWorkItemId.ToString();
	        }

		  foreach (Field f in w.Fields)
          {
            if (f.Value is string)
            {
              ri.Fields.Add(f.Name, HtmlRemoval.StripTagsRegex(f.Value as string));
            }
            else
            {
              ri.Fields.Add(f.Name, f.Value);
            }
          }
          // Add extra fields
          ri.Fields.Add("IterationPath", w.IterationPath);
          ri.Fields.Add("AreaPath", w.AreaPath);
          l.Add(ri);
        }
        return l;
      }
    }

    private const string descriptionKey = "Description";
    private const string descriptionHtmlKey = "Description HTML";

    private string GetDescription(WorkItem workItem)
    {
      if (workItem.Fields.Contains(descriptionKey))
      {
        var s = workItem.Fields[descriptionKey].Value.ToString();
        return HtmlRemoval.StripTagsRegex(s);
      }

      if (workItem.Fields.Contains(descriptionHtmlKey))
      {
        var s = workItem.Fields[descriptionHtmlKey].Value.ToString();
        return HtmlRemoval.StripTagsRegex(s);
      }
      return string.Empty;
    }
  }
}
