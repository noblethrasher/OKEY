using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.Collections;
using System.Security.Principal;
using System.Globalization;
using System.Runtime.InteropServices;

namespace OKey
{
   public class OkeyUser : IPrincipal
   {
        public bool Authenticated { get; private set; }

        public string GivenName  { get; private set; }
        public string Surname    { get; private set; }
        public string Email      { get; private set; }
        public string Phone      { get; private set; }
        public string OkeyID     { get; private set; }
        public string Department { get; private set; }
        public string CWID       { get; private set; }
        public string ShortName  { get; private set; }
        public string Title      { get; private set; }
        public string FullName   { get { return GivenName + " " + Surname; } }

        public IEnumerable<string> Roles
        {
            get;
            private set;
        }

        public OkeyUser(SearchResult result)
        {
            Init (result);
        }

        public OkeyUser(string user, string pass)
        {
            const string PATH = "LDAP://ad.okstate.edu/DC=ad,DC=okstate,DC=edu";

            var entry = new DirectoryEntry (PATH, user, pass);

            var attributeToSearch = user.EndsWith ("@okstate.edu", StringComparison.OrdinalIgnoreCase) ? "userPrincipalName" : "name";
            
            var searcher = new DirectorySearcher (entry, string.Format ("({1}={0})", user.ToLower (), attributeToSearch));

            try
            {
                var nativeObject = entry.NativeObject;
                Init (searcher.FindOne ());
                
            }
            catch (DirectoryServicesCOMException ex)
            {

            }
            catch (COMException ex)
            {

            }
            finally
            {
                searcher.Dispose ();
                entry.Close ();
            }
        }

        void Init(SearchResult result)
        {
            
            Func<string, string> _r = s =>
            {
                string p = "";

                foreach (var t in result.Properties[s])
                    p += t;

                return p;
            };

            Email        = _r ("mail");
            GivenName    = _r ("givenName");
            Surname      = _r ("sn");
            Phone        = _r ("telephoneNumber");
            OkeyID       = _r ("userPrincipalName");
            Department   = _r ("department");
            CWID         = _r ("CWID");
            ShortName    = _r ("name");
            Title        = _r ("title");

            Roles = new InternalList () { xs = _r ("memberOf").Split (',') };

            id = new InternalIdClass (this);

            Authenticated = true;

        }

        public static bool operator true(OkeyUser user)
        {
            return user.Authenticated;
        }

        public static bool operator false(OkeyUser user)
        {
            return user.Authenticated;
        }

        //internal ienumerable<string> class to preempt the possibility of casting to the underlying array
        class InternalList : IEnumerable<string>
        {
            public string[] xs;

            public IEnumerator<string> GetEnumerator()
            {
                foreach (var x in xs)
                    yield return x;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator ();
            }
        }

        class InternalIdClass : IIdentity
        {
            OkeyUser user;

            public InternalIdClass(OkeyUser user)
            {
                this.user = user;
            }

            public string AuthenticationType
            {
                get { return "Active Directory"; }
            }

            public bool IsAuthenticated
            {
                get { return user.Authenticated; }
            }

            public string Name
            {
                get { return user.Authenticated ? user.FullName : "Unauthenticated User"; }
            }
        }

        InternalIdClass id;

        System.Security.Principal.IIdentity System.Security.Principal.IPrincipal.Identity
        {
            get { return id; }
        }

        class _strComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x.Equals (y, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return obj.GetHashCode ();
            }
        } 

        _strComparer comparer = new _strComparer ();

        bool System.Security.Principal.IPrincipal.IsInRole(string role)
        {
            return Roles.Contains (role, comparer);
        }        
    }
}
