﻿
namespace Bytewizer.Backblaze.Models
{
    /// <summary>
    /// Represent file action status.
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Indicates a large file has been started but not finished or canceled.
        /// </summary>
        Start,

        /// <summary>
        /// Indicates a file that was uploaded to B2 Cloud Storage.
        /// </summary>
        Upload,

        /// <summary>
        /// Marking the file as hidden it will not show up in list file names.
        /// </summary>
        Hide,

        /// <summary>
        /// Virtual folder when listing files.
        /// </summary>
        Folder
    }
}