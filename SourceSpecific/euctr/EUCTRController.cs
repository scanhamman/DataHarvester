﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DataHarvester.euctr
{
    class EUCTRController
	{
		DataLayer common_repo;
		LoggingDataLayer logging_repo;
		EUCTRProcessor processor;
		Source source;
		int harvest_type_id;
		int last_harvest_id;

		public EUCTRController(int _last_harvest_id, Source _source, DataLayer _common_repo, LoggingDataLayer _logging_repo, int _harvest_type_id)
		{
			source = _source;
			processor = new EUCTRProcessor();
			common_repo = _common_repo;
			logging_repo = _logging_repo;
			harvest_type_id = _harvest_type_id;
			last_harvest_id = _last_harvest_id;
		}

		public int? LoopThroughFiles()
		{
     		// Construct a list of the files 
			// Rather than using a file base, it is possible
			// to use the sf records to get a list of files
			// and local paths...

			IEnumerable<StudyFileRecord> file_list = logging_repo.FetchStudyFileRecords(source.id, harvest_type_id);
			int n = 0; string filePath = "";
			foreach (StudyFileRecord rec in file_list)
			{
				n++;
				// for testing...
				// if (n == 500) break;

				filePath = rec.local_path;
				if (File.Exists(filePath))
				{
					string inputString = "";
					using (var streamReader = new StreamReader(filePath, System.Text.Encoding.UTF8))
					{
						inputString += streamReader.ReadToEnd();

						// at least one file has this odd ('start of text') character, 
						// which throws an error in the deserialisation process
						inputString = inputString.Replace("&#x2;", "");
					}

					try
					{
						XmlSerializer serializer = new XmlSerializer(typeof(EUCTR_Record));
						StringReader rdr = new StringReader(inputString);
						EUCTR_Record studyRegEntry = (EUCTR_Record)serializer.Deserialize(rdr);

						// break up the file into relevant data classes
						Study s = processor.ProcessData(studyRegEntry, rec.last_downloaded, common_repo, logging_repo);

						// check and store data object links - just pdfs for now
						// (commented out for the moment to save time during extraction).
						// await HtmlHelpers.CheckURLsAsync(s.object_instances);

						// store the data in the database
						processor.StoreData(common_repo, s, logging_repo);

						// update file record with last processed datetime
						logging_repo.UpdateFileRecLastHarvested(rec.id, "study", last_harvest_id);
					}

					catch (Exception e)
					{
						logging_repo.LogError("In main processing loop, record number " + n.ToString() + ": " + e.Message);
					}

				}

				if (n % 10 == 0) logging_repo.LogLine(n.ToString());
			}
			return n;
		}
	}
}
