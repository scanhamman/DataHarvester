﻿using Dapper.Contrib.Extensions;
using Dapper;
using Npgsql;
using System;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections.Generic;
using PostgreSQLCopyHelper;

namespace DataHarvester
{
	public class OrgUpdater
	{
		private string connString;
		private Source source;

		public OrgUpdater(string _connString, Source _source)
		{
			connString = _connString;
			source = _source;
		}


		public void EstablishContextForeignTables(string user_name, string password)
        {
			using (var conn = new NpgsqlConnection(connString))
			{
				string sql_string = @"CREATE EXTENSION IF NOT EXISTS postgres_fdw
			                         schema sd;";
                conn.Execute(sql_string);

     			sql_string = @"CREATE SERVER IF NOT EXISTS context "
						   + @" FOREIGN DATA WRAPPER postgres_fdw
                             OPTIONS (host 'localhost', dbname 'context', port '5432');";
				conn.Execute(sql_string);

				sql_string = @"CREATE USER MAPPING IF NOT EXISTS FOR CURRENT_USER
                     SERVER context 
					 OPTIONS (user '" + user_name + "', password '" + password + "');";
				conn.Execute(sql_string);

				sql_string = @"DROP SCHEMA IF EXISTS context_ctx cascade;
                     CREATE SCHEMA context_ctx; 
                     IMPORT FOREIGN SCHEMA ctx
                     FROM SERVER context 
                     INTO context_ctx;";
				conn.Execute(sql_string);
			}
		}

		public void UpdateStudyIdentifierOrgs()
		{
			OrgIdHelper helper = new OrgIdHelper(connString);
			helper.update_study_identifiers_using_default_name();
			helper.update_study_identifiers_using_other_name();
			helper.update_study_identifiers_insert_default_names();
			helper.store_unmatched_study_identifiers_org_names(source.id);
		}


		public void UpdateStudyContributorOrgs()
		{
			if (source.has_study_contributors)
			{
				OrgIdHelper helper = new OrgIdHelper(connString);
				helper.update_study_contributors_using_default_name();
				helper.update_study_contributors_using_other_name();
				helper.update_study_contributors_insert_default_names();
				helper.store_unmatched_study_contributors_org_names(source.id);
			}
		}

		public void UpdateDataObjectOrgs()
		{
			OrgIdHelper helper = new OrgIdHelper(connString);
			helper.update_data_objects_using_default_name();
			helper.update_data_objects_using_other_name();
			helper.update_data_objects_insert_default_names();
			helper.store_unmatched_data_object_org_names(source.id);
		}


		public void DropContextForeignTables()
		{
			using (var conn = new NpgsqlConnection(connString))
			{
				string sql_string = @"DROP USER MAPPING IF EXISTS FOR CURRENT_USER
                     SERVER context;";
				conn.Execute(sql_string);

				sql_string = @"DROP SERVER IF EXISTS context CASCADE;";
				conn.Execute(sql_string);

				sql_string = @"DROP SCHEMA IF EXISTS context_ctx;";
				conn.Execute(sql_string);
			}
		}
	}
}

