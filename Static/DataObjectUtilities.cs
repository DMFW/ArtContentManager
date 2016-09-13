using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Reflection;

namespace ArtContentManager.Static
{
    public class DataObjectUtilities
    {
       public static void LoadObjectWithDataRow(DataColumnCollection p_dcc, DataRow p_dr, Object p_object)
        {
            //This is used to do the reflection
            Type t = p_object.GetType();
            for (Int32 i = 0; i <= p_dcc.Count - 1; i++)
            {
                try
                {

                    // NOTE the datarow column names must match exactly 
                    // (including case) to the object property names

                    t.InvokeMember(p_dcc[i].ColumnName,
                                  BindingFlags.SetProperty, null,
                                  p_object,
                                  new object[] { p_dr[p_dcc[i].ColumnName] });
                }
                catch (Exception ex)
                {
                    // Usually you are getting here because a column 
                    // doesn't exist or it is null

                    if (ex.ToString() != null)
                    { }
                }
            }
        }

        public static void LoadDataRowWithObject(Object p_obj, ref DataTable p_dt, ref DataRow p_dr)
        {
            // We need the type to figure out the properties

            Type t = p_obj.GetType();

            // Get the properties of our type

            PropertyInfo[] tmpP = t.GetProperties();

            // We need to create the table if it doesn't already exist

            if (p_dt == null)
            {
                p_dt = new DataTable();

                // Create the columns of the table based off the 
                // properties we reflected from the type

                foreach (PropertyInfo xtemp2 in tmpP)
                {
                    if (Attribute.IsDefined(xtemp2, typeof(DataRowField)))
                    {
                        p_dt.Columns.Add(xtemp2.Name,
                                   xtemp2.PropertyType);
                    }
                } 
            }

            // Now the table should exist so add records to it.

            // First extract those properies (and only those properties) that are tagged
            // as DataRowField properties. These are contracted to match fields in the DataTable
            // and so must be named as per the names on the SQL database.

            Dictionary<string, object> dctPropertiesToApply = new Dictionary<string, object>();

            foreach (PropertyInfo xtemp2 in tmpP) 
            {
                if (Attribute.IsDefined(xtemp2, typeof(DataRowField)))
                {
                    dctPropertiesToApply.Add
                        (xtemp2.Name,
                        t.InvokeMember(xtemp2.Name, BindingFlags.GetProperty, null, p_obj, new object[0]));
                }
            }

            // Add a row to the table if we are requesting a new one

            if (p_dr == null) { p_dr = p_dt.NewRow(); }

            // And add all the mapped fields

            foreach (KeyValuePair<string, object> kvpPropertyToApply in dctPropertiesToApply)
            {
                p_dr[kvpPropertyToApply.Key] = kvpPropertyToApply.Value;
            }

        }

    }
}
