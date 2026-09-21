using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sovoma;

namespace ZusiObjektAlbum.Sovoma
{
  public class BaseTreeViewViewModel<T, O> : ObservableObject
  {
    private bool _bold;
    private bool _dirty;
    private bool _expanded;
    private bool _filterVisible;
    private bool _selected;

    protected ObservableCollection<T> _children = new();
    protected bool _anything;
    protected string _description;
    protected string _displayName;
    protected T _parent;
    protected O _object;

    public ObservableCollection<T> Children { get => _children; }

    public string Description { get => _description; }

    public string DisplayName
    {
      get => _displayName;
      set
      {
        if (_displayName != value)
        {
          _displayName = value;
          RaisePropertyChanged(nameof(DisplayName));
        }
      }
    }

    public bool Anything
    {
      get => _anything;
      set
      {
        if (_anything != value)
        {
          _anything = value;
          RaisePropertyChanged(nameof(Anything));
        }
      }
    }

    public bool IsBold
    {
      get => _bold;
      set
      {
        if (_bold != value)
        {
          _bold = value;
          RaisePropertyChanged(nameof(IsBold));
        }
      }
    }

    public bool IsDirty
    {
      get => GetIsDirty();
      set
      {
        if (_dirty != value)
        {
          _dirty = value;
          RaisePropertyChanged(nameof(IsDirty));
        }
      }
    }

    public bool IsExpanded
    {
      get => _expanded;
      set
      {
        if (_expanded != value)
        {
          _expanded = value;
          RaisePropertyChanged(nameof(IsExpanded));
        }
      }
    }

    public bool IsSelected
    {
      get => _selected;
      set
      {
        if (_selected != value)
        {
          _selected = value;
          if (value)
          {
            DeselectChildren();
          }
          SelectedChanged(value);
          RaisePropertyChanged(nameof(IsSelected));
        }
      }
    }

    public bool IsFilterVisible
    {
      get => _filterVisible;
      set
      {
        if (_filterVisible != value)
        {
          _filterVisible = value;
          RaisePropertyChanged(nameof(IsFilterVisible));
        }
      }
    }

    public O Object
    {
      get => _object;
      set
      {
        _object = value;
        RaisePropertyChanged(nameof(Object));
      }
    }

    public T Parent { get => _parent; }

    //---------------------------------------------------------------------
    public BaseTreeViewViewModel(T parent, bool expanded, bool selected, bool visible)
    {
      _parent = parent;
      _expanded = expanded;
      _filterVisible = visible;
      _selected = selected;
    }

    //---------------------------------------------------------------------
    public virtual void Initialize()
    {
      foreach (T child in _children)
      {
        BaseTreeViewViewModel<T, O> btvm = child as BaseTreeViewViewModel<T, O>;
        btvm._parent = (T)(object)this;
        btvm.Initialize();
      }
    }

    /*
    public void Sort(Comparison<T> comparison);
    public void Sort(int index, int count, IComparer<T>? comparer);
    public void Sort();
    public void Sort(IComparer<T>? comparer);
     */

    //---------------------------------------------------------------------
    public virtual void Initialize(Comparison<T> comparison)
    {
      List<T> tmp = new(_children);
      tmp.Sort(comparison);
      _children.Clear();
      tmp.ForEach(t => _children.Add(t));

      foreach (T child in _children)
      {
        BaseTreeViewViewModel<T, O> btvm = child as BaseTreeViewViewModel<T, O>;
        btvm._parent = (T)(object)this;
        btvm.Initialize(comparison);
      }
    }

    //---------------------------------------------------------------------
    public void InsertChildren(T child)
    {
      _children.Add(child);
      Initialize();
    }

    //---------------------------------------------------------------------
    public void InsertChildren(T child, Comparison<T> comparison)
    {
      int ix = -1;
      foreach (T cvm in _children)
      {
        if (comparison?.Invoke(cvm, child) >= 0)
        {
          ix = _children.IndexOf(cvm);
          break;
        }
      }
      if (ix >= 0)
      {
        _children.Insert(ix, child);
      }
      else
      {
        _children.Add(child);
      }

      Initialize();
    }

    //---------------------------------------------------------------------
    public T SearchByDisplayName(string displayName)
    {
      if (string.Compare(_displayName, displayName, true) == 0)
      {
        return (T)(object)this;
      }

      foreach (T cvm in _children)
      {
        T svm = (cvm as BaseTreeViewViewModel<T, O>).SearchByDisplayName(displayName);
        if (svm is not null)
        {
          return svm;
        }
      }

      return default;
    }

    //---------------------------------------------------------------------
    protected virtual void SelectedChanged(bool value)
    { }

    //---------------------------------------------------------------------
    private void DeselectChildren()
    {
      _children.ForEach(c => (c as BaseTreeViewViewModel<T, O>).IsSelected = false);
    }

    //---------------------------------------------------------------------
    private bool GetIsDirty()
    {
      if (_dirty)
        return true;

      foreach (T t in _children)
      {
        if ((t as BaseTreeViewViewModel<T, O>).GetIsDirty())
          return true;
      }

      return false;
    }
  }
}
