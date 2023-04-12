//  Copyright (c) 2006, Gustavo Franco
//  Email:  gustavo_franco@hotmail.com
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  Redistributions of source code must retain the above copyright notice, 
//  this list of conditions and the following disclaimer. 
//  Redistributions in binary form must reproduce the above copyright notice, 
//  this list of conditions and the following disclaimer in the documentation 
//  and/or other materials provided with the distribution. 

//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.

using IconLib.Exceptions;
using IconLib.LibraryFormats;

namespace IconLib
{
    public class MultiIcon : List<SingleIcon>
    {
        #region Variables Declaration

        private int mSelectedIndex = -1;

        #endregion

        #region Constructors

        public MultiIcon()
        {
        }

        public MultiIcon(IEnumerable<SingleIcon> collection)
        {
            this.AddRange(collection);
        }

        public MultiIcon(SingleIcon singleIcon)
        {
            this.Add(singleIcon);
            this.SelectedName = singleIcon.Name;
        }

        #endregion

        #region Properties

        public int SelectedIndex
        {
            get => mSelectedIndex;
            set
            {
                if (value >= this.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(this.SelectedIndex));
                }

                mSelectedIndex = value;
            }
        }

        public string SelectedName
        {
            get
            {
                if (mSelectedIndex < 0 || mSelectedIndex >= this.Count)
                {
                    return null;
                }

                return this[mSelectedIndex].Name;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(this.SelectedName));
                }

                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].Name.ToLower() == value.ToLower())
                    {
                        mSelectedIndex = i;
                        return;
                    }
                }

                throw new InvalidDataException("SelectedName does not exist.");
            }
        }

        public string[] IconNames
        {
            get
            {
                var names = new List<string>();
                foreach (var icon in this)
                {
                    names.Add(icon.Name);
                }

                return names.ToArray();
            }
        }

        #endregion

        #region Indexers

        public SingleIcon this[string name]
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].Name.ToLower() == name.ToLower())
                    {
                        return this[i];
                    }
                }

                return null;
            }
        }

        #endregion

        #region Public Methods

        public SingleIcon Add(string iconName)
        {
            // Already exist?
            if (this.Contains(iconName))
            {
                throw new IconNameAlreadyExistException();
            }

            // Lets Create the icon group
            // Add group to the master list and also lets give a name
            var singleIcon = new SingleIcon(iconName);
            this.Add(singleIcon);
            return singleIcon;
        }

        public void Remove(string iconName)
        {
            if (iconName == null)
            {
                throw new ArgumentNullException(nameof(iconName));
            }

            // If not exist then do nothing
            int index = this.IndexOf(iconName);
            if (index == -1)
            {
                return;
            }

            this.RemoveAt(index);
        }

        public bool Contains(string iconName)
        {
            if (iconName == null)
            {
                throw new ArgumentNullException(nameof(iconName));
            }

            // Exist?
            return this.IndexOf(iconName) != -1 ? true : false;
        }

        public int IndexOf(string iconName)
        {
            if (iconName == null)
            {
                throw new ArgumentNullException(nameof(iconName));
            }

            // Exist?
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i].Name.ToLower() == iconName.ToLower())
                {
                    return i;
                }
            }

            return -1;
        }

        public void Load(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            try
            {
                this.Load(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        public void Load(Stream stream)
        {
            IconFormat baseFormat;

            if ((baseFormat = new IconFormat()).IsRecognizedFormat(stream))
            {
                if (mSelectedIndex == -1)
                {
                    this.Clear();
                    this.Add(baseFormat.Load(stream)[0]);
                    this[0].Name = "Untitled";
                }
                else
                {
                    string currentName = this[mSelectedIndex].Name;
                    this[mSelectedIndex] = baseFormat.Load(stream)[0];
                    this[mSelectedIndex].Name = currentName;
                }
            }
            else
            {
                throw new InvalidFileException();
            }

            this.SelectedIndex = this.Count > 0 ? 0 : -1;
        }

        public void Save(string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            try
            {
                this.Save(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
        }

        public void Save(Stream stream)
        {
            if (mSelectedIndex == -1)
            {
                throw new InvalidIconSelectionException();
            }

            new IconFormat().Save(this, stream);
        }

        #endregion

        #region Private Methods

        private void CopyFrom(MultiIcon multiIcon)
        {
            mSelectedIndex = multiIcon.mSelectedIndex;
            this.Clear();
            this.AddRange(multiIcon);
        }

        #endregion
    }
}