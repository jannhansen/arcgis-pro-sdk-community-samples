﻿//   Copyright 2014 Esri
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//       http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 

using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace TimeNavigation
{
    /// <summary>
    /// This sample provides a new tab and controls that allow you to set the time in the map view, step through time, and navigate between time enabled bookmarks in the map.
    /// </summary>
    /// <remarks>
    /// 1. In Visual Studio click the Build menu. Then select Build Solution.
    /// 2. Click Start button to open ArcGIS Pro.
    /// 3. ArcGIS Pro will open. 
    /// 4. Open a map view that contains time aware data. Click on the new Navigation tab within the Time tab group on the ribbon.  
    /// ![UI](screenshots/UICommands.png)  
    /// 5. Within this tab there are 3 groups that provide functionality to navigate through time.
    /// 6. The Map Time group provides two date picker controls to set the start and end time in the map.
    /// 7. The Time Step group provides two combo boxes to set the time step interval. The previous and next button can be used to offset the map time forward or back by the specified time step interval.
    /// 8. The Bookmarks group provides a gallery of time enabled bookmarks for the map. Clicking a bookmark in the gallery will zoom the map to that location and time. 
    /// It also provides play, previous and next buttons that can be used to navigate between the time enabled bookmarks. 
    /// These commands are only enabled when there are at least 2 bookmarks in the map. Finally it provides a slider that can be used to set how quickly to move between bookmarks during playback.
    /// </remarks>
  internal class TimeModule : Module
  {
    private static TimeModule _this = null;
    private static Settings _settings = Settings.Default;
    private static ReadOnlyObservableCollection<Bookmark> _bookmarks;

    public TimeModule()
    {
      _bookmarks = new ReadOnlyObservableCollection<Bookmark>(new ObservableCollection<Bookmark>());
      MapViewTimeChangedEvent.Subscribe(OnTimeChanged);
      ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);     
      LoadBookmarks();
    }

    ~TimeModule()
    {
      _settings.Save();
      MapViewTimeChangedEvent.Unsubscribe(OnTimeChanged);
      ActiveMapViewChangedEvent.Unsubscribe(OnActiveMapViewChanged);
    }

    /// <summary>
    /// Retrieve the singleton instance to this module here
    /// </summary>
    public static TimeModule Current
    {
      get
      {
        return _this ?? (_this = (TimeModule)FrameworkApplication.FindModule("TimeNavigation_Module"));
      }
    }

    public static Settings Settings
    {
      get { return _settings; }
    }

    #region Bookmarks

    public static List<Bookmark> Bookmarks
    {
      get { return new List<Bookmark>(_bookmarks.Where(b => b.HasTimeExtent)); }
    }

    public static Bookmark CurrentBookmark { get; set; }

    public static Bookmark GetNextBookmark()
    {
      var bookmarks = TimeModule.Bookmarks;

      int i = 0;
      if (TimeModule.CurrentBookmark != null)
      {
        i = bookmarks.IndexOf(TimeModule.CurrentBookmark) + 1;
        if (i >= bookmarks.Count)
          i = 0;
      }
      TimeModule.CurrentBookmark = bookmarks.ElementAt(i);
      return TimeModule.CurrentBookmark;
    }

    public static Bookmark GetPreviousBookmark()
    {
      var bookmarks = TimeModule.Bookmarks;

      int i = bookmarks.Count - 1;
      if (TimeModule.CurrentBookmark != null)
      {
        i = bookmarks.IndexOf(TimeModule.CurrentBookmark) - 1;
        if (i < 0)
          i = bookmarks.Count - 1;
      }
      TimeModule.CurrentBookmark = bookmarks.ElementAt(i);
      return TimeModule.CurrentBookmark;
    }

    private async Task LoadBookmarks()
    {
      if (MapView.Active != null)
        _bookmarks = await QueuedTask.Run(() => MapView.Active.Map.GetBookmarks());
      else
        _bookmarks = new ReadOnlyObservableCollection<Bookmark>(new ObservableCollection<Bookmark>());
    }

    #endregion

    #region Map Time Enabled Condition

    private const string mapTimeEnabledState = "TimeNavigation_MapTimeEnabledState";
    private void SetMapTimeEnabledState(bool isEnabled)
    {
      Pane activePane = FrameworkApplication.Panes.ActivePane;
      if (activePane == null)
        return;

      if (isEnabled)
        activePane.State.Activate(mapTimeEnabledState);
      else
        activePane.State.Deactivate(mapTimeEnabledState);
    }

    #endregion

    #region Event Handlers

    private void OnTimeChanged(MapViewTimeChangedEventArgs obj)
    {
      SetMapTimeEnabledState((obj.MapView != null && obj.MapView.Time != null));
    }

    private void OnActiveMapViewChanged(MapViewEventArgs obj)
    {
      SetMapTimeEnabledState((obj.MapView != null && obj.MapView.Time != null));
      LoadBookmarks();
    }

    #endregion
  }
}
