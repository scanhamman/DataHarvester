﻿using Dapper.Contrib.Extensions;
using Dapper;
using Npgsql;
using System;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections.Generic;

namespace DataHarvester.Yoda
{
	public class YodaDataLayer
	{
		private string _mon_connString;
		private string _biolincc_pp_connString;
		private string _ctg_connString;
		private string _isrctn_connString;
		private string _yoda_pp_connString;

		/// <summary>
		/// Parameterless constructor is used to automatically build
		/// the connection string, using an appsettings.json file that 
		/// has the relevant credentials (but which is not stored in GitHub).
		/// The json file also includes the root folder path, which is
		/// stored in the class's folder_base property.
		/// </summary>
		public YodaDataLayer()
		{
			IConfigurationRoot settings = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json")
				.Build();

			NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
			builder.Host = settings["host"];
			builder.Username = settings["user"];
			builder.Password = settings["password"];

			builder.Database = "mon";
			builder.SearchPath = "sf";
			_mon_connString = builder.ConnectionString;

			builder.Database = "biolincc";
			builder.SearchPath = "pp";
			_biolincc_pp_connString = builder.ConnectionString;

			builder.Database = "ctg";
			builder.SearchPath = "ad";
			_ctg_connString = builder.ConnectionString;

			builder.Database = "isrctn";
			builder.SearchPath = "ad";
			_isrctn_connString = builder.ConnectionString;

			builder.Database = "yoda";
			builder.SearchPath = "pp";
			_yoda_pp_connString = builder.ConnectionString;

			// example appsettings.json file...
			// the only values required are for...
			// {
			//	  "host": "host_name...",
			//	  "user": "user_name...",
			//    "password": "user_password...",
			//	  "folder_base": "C:\\MDR JSON\\Object JSON... "
			// }
		}


		// get listing of local file paths
		public IEnumerable<string> FetchFilePaths(int source_id)
		{
			using (NpgsqlConnection Conn = new NpgsqlConnection(_mon_connString))
			{
				string sql_string = "select local_path ";
				sql_string += " from sf.source_data_studies ";
				sql_string += " where source_id = " + source_id.ToString();
				sql_string += " and local_path is not null";
				sql_string += " order by local_path";
				return Conn.Query<string>(sql_string);
			}
		}

		// get record of interest
		public FileRecord FetchFileRecord(string sd_id, int source_id)
		{
			using (NpgsqlConnection Conn = new NpgsqlConnection(_mon_connString))
			{
				string sql_string = "select id, source_id, sd_id, remote_url, last_sf_id, last_revised, ";
				sql_string += " assume_complete, download_status, download_datetime, local_path ";
				sql_string += " from sf.source_data_studies ";
				sql_string += " where sd_id = '" + sd_id + "' and source_id = " + source_id.ToString();
				return Conn.Query<FileRecord>(sql_string).FirstOrDefault();
			}
		}

		public bool StoreFileRec(FileRecord file_record)
		{
			using (var conn = new NpgsqlConnection(_mon_connString))
			{
				return conn.Update<FileRecord>(file_record);
			}
		}

		public int InsertFileRec(FileRecord file_record)
		{
			using (var conn = new NpgsqlConnection(_mon_connString))
			{
				return (int)conn.Insert<FileRecord>(file_record);
			}
		}

		public SponsorDetails FetchYodaSponsorFromNCT(string nct_id)
		{
			using (var conn = new NpgsqlConnection(_ctg_connString))
			{
				string sql_string = "Select organisation_id as org_id, organisation_name as org_name from ad.study_contributors ";
				sql_string += "where sd_id = '" + nct_id + "' and contrib_type_id = 54;";
				return conn.QueryFirstOrDefault<SponsorDetails>(sql_string);
			}
		}


		public SponsorDetails FetchYodaSponsorFromISRCTN(string isrctn_id)
		{
			using (var conn = new NpgsqlConnection(_isrctn_connString))
			{
				string sql_string = "Select organisation_id as org_id, organisation_name as org_name from ad.study_contributors ";
				sql_string += "where sd_id = '" + isrctn_id + "' and contrib_type_id = 54;";
				return conn.QueryFirstOrDefault<SponsorDetails>(sql_string);
			}
		}


		public SponsorDetails FetchYodaSponsorDetailsFromTable(string sd_id)
		{
			using (var conn = new NpgsqlConnection(_yoda_pp_connString))
			{
				string sql_string = "Select sponsor_org_id as org_id, sponsor_org as org_name from pp.not_registered ";
				sql_string += "where link_param = '" + sd_id + "'";
				return conn.QueryFirstOrDefault<SponsorDetails>(sql_string);
			}
		}
	}
}