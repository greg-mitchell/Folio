using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;

namespace Folio
{
    class CardCollectionBindingList : CollectionBase , IBindingList
    {
        private List<CardCollectionRecord> _cards;
        private ListChangedEventArgs _resetEvent;
        private ListChangedEventHandler _onListChanged;

        public CardCollectionBindingList()
        {
            _cards = new List<CardCollectionRecord>();
            _resetEvent = new ListChangedEventArgs(ListChangedType.Reset, -1);
        }

        public CardCollectionRecord this[int index]
        {
            get { return _cards[index]; }
            set { _cards[index] = value; }
        }

        public int Add(CardCollectionRecord value)
        {
            _cards.Add(value);
            return _cards.IndexOf(value);
        }

        public object AddNew()
        {
            return ((IBindingList)this).AddNew();
        }

        public void Remove(CardCollectionRecord value)
        {
            _cards.Remove(value);
        }

        protected virtual void OnListChanged(ListChangedEventArgs ev)
        {
            if (_onListChanged != null)
                _onListChanged(this, ev);
        }

        protected override void OnClear()
        {
            base.OnClear();
        }

        protected override void OnClearComplete()
        {
            OnListChanged(_resetEvent);
        }

        protected override void OnInsertComplete(int index, object value)
        {
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
        }

        protected override void OnRemoveComplete(int index, object value)
        {
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
        }

        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            if(oldValue != newValue)
            {
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
            }
        }

        internal void CardChanged(CardCollectionRecord card)
        {
            int index = _cards.IndexOf(card);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
        }

        // implements IBindingList
        public void AddIndex(PropertyDescriptor property)
        {
            throw new NotImplementedException();
        }

        public bool AllowEdit
        {
            get { return true; }
        }

        public bool AllowNew
        {
            get { return true; }
        }

        public bool AllowRemove
        {
            get { return true; }
        }

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotImplementedException();
        }

        public int Find(PropertyDescriptor property, object key)
        {
            throw new NotImplementedException();
        }

        public bool IsSorted
        {
            get { throw new NotImplementedException(); }
        }

        public event ListChangedEventHandler ListChanged;

        public void RemoveIndex(PropertyDescriptor property)
        {
            throw new NotImplementedException();
        }

        public void RemoveSort()
        {
            throw new NotImplementedException();
        }

        public ListSortDirection SortDirection
        {
            get { throw new NotImplementedException(); }
        }

        public PropertyDescriptor SortProperty
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportsChangeNotification
        {
            get { return true; }
        }

        public bool SupportsSearching
        {
            get { throw new NotImplementedException(); }
        }

        public bool SupportsSorting
        {
            get { return true;  }
        }

        public class CardCollectionRecord
        {
            public int Quantity { get; set; }
            public Card Card { get; set; }
        }
    }
}
