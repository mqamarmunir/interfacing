#region Using Namespaces...

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data.Entity.Validation;

#endregion

namespace DataModel
{
    /// <summary>
    /// Unit of Work class responsible for DB transactions
    /// </summary>
    public class UnitOfWork : IDisposable
    {
        #region Private member variables...

        private miEntities _context = null;
        private GenericRepository<mi_ttests> _testRepository;
        private GenericRepository<mi_ttestattribute> _attribueRepository;
        private GenericRepository<cliqmachinemapping> _cliqMachineMappings;
        private GenericRepository<mi_tinstruments> _InstrumentsRepository;
        private GenericRepository<mi_tresult> _ResultsRepository;

        
        //private GenericRepository<Token> _tokenRepository;
        #endregion

        public UnitOfWork()
        {
            _context = new miEntities();
        }

        #region Public Repository Creation properties...
        
         public GenericRepository<mi_tresult> ResultsRepository
        {
            get
            {
                if (this._ResultsRepository == null)
                    this._ResultsRepository = new GenericRepository<mi_tresult>(_context);
                return _ResultsRepository;
            }
        }
            /// <summary>
        /// Get/Set Property for product repository.
        /// </summary>
        public GenericRepository<mi_ttestattribute> _TestAttributeRepository
        {
            get
            {
                if (this._attribueRepository == null)
                    this._attribueRepository = new GenericRepository<mi_ttestattribute>(_context);
                return _attribueRepository;
            }
        }
        public GenericRepository<mi_tinstruments> InstrumentsRepository
        {
            get
            {
                if (this._InstrumentsRepository == null)
                    this._InstrumentsRepository = new GenericRepository<mi_tinstruments>(_context);
                return _InstrumentsRepository;
            }
           
        }
        /// <summary>
        /// Get/Set Property for cliqmachinemappings.
        /// </summary>
        public GenericRepository<cliqmachinemapping> CliqMachineMappings
        {
            get
            {
                if (this._cliqMachineMappings == null)
                    this._cliqMachineMappings = new GenericRepository<cliqmachinemapping>(_context);
                return _cliqMachineMappings;
            }
        }

        /// <summary>
        /// Get/Set Property for user repository.
        /// </summary>
        public GenericRepository<mi_ttests> _TestRepository
        {
            get
            {
                if (this._testRepository == null)
                    this._testRepository = new GenericRepository<mi_ttests>(_context);
                return _testRepository;
            }
        }

     
        #endregion

        #region Public member methods...
        /// <summary>
        /// Save method.
        /// </summary>
        public void Save()
        {
            try
            {
                _context.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {

                var outputLines = new List<string>();
                foreach (var eve in e.EntityValidationErrors)
                {
                    outputLines.Add(string.Format(
                        "{0}: Entity of type \"{1}\" in state \"{2}\" has the following validation errors:", DateTime.Now, 
                        eve.Entry.Entity.GetType().Name, eve.Entry.State));
                    foreach (var ve in eve.ValidationErrors)
                    {
                        outputLines.Add(string.Format("- Property: \"{0}\", Error: \"{1}\"", ve.PropertyName, ve.ErrorMessage));
                    }
                }
                System.IO.File.AppendAllLines(@"C:\errors.txt", outputLines);

                throw e;
            }

        }

        #endregion

        #region Implementing IDiosposable...

        #region private dispose variable declaration...
        private bool disposed = false; 
        #endregion

        /// <summary>
        /// Protected Virtual Dispose method
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Debug.WriteLine("UnitOfWork is being disposed");
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        } 
        #endregion
    }
}