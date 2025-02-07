﻿using Dexter.Abstractions;

namespace Dexter.Configurations
{

    /// <summary>
    /// The GreetFurConfiguration specifies the information relating to the Google Sheets data required.
    /// </summary>

    public class GreetFurConfiguration : JSONConfig
    {

        /// <summary>
        /// The ApplicationName is the name of the application specified when creating the credentials.json file.
        /// </summary>

        public string ApplicationName { get; set; }

        /// <summary>
        /// The CredentialFile is the place the credential file generated by Google is stored.
        /// </summary>

        public string CredentialFile { get; set; }

        /// <summary>
        /// The TokenFile is the place the token file generated by the program from the credentials is stored.
        /// </summary>

        public string TokenFile { get; set; }

        /// <summary>
        /// The SpreadSheetID is the ID of the GreetFur spreadsheet chart.
        /// </summary>

        public string SpreadSheetID { get; set; }

        /// <summary>
        /// The FortnightSpreadsheet represents the title of the spreadsheet containing all the fortnightly data for the GreetFur.
        /// </summary>

        public string FortnightSpreadsheet { get; set; }

        /// <summary>
        /// The TheBigPictureSpreadsheet represents the title of the spreadsheet containing all the records for the GreetFurs.
        /// </summary>

        public string TheBigPictureSpreadsheet { get; set; }

        /// <summary>
        /// The IDColumnIndex is the index of the column that contains all the UserIDs.
        /// </summary>

        public string IDColumnIndex { get; set; }

        /// <summary>
        /// The TotalID is the index of the column that contains all the total amounts for the Big Picture spreadsheet.
        /// </summary>

        public string TotalID { get; set; }

        /// <summary>
        /// The Information dictionary stores all the column indexes and their respective names.
        /// </summary>

        public Dictionary<string, int> Information { get; set; }

        /// <summary>
        /// The AWOO role ID is used for finding if a GreetFur is attempting to mute someone already in the server.
        /// </summary>

        public ulong AwooRole { get; set; }

    }

}
