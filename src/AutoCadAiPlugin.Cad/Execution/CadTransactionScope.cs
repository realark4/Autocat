using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutoCadAiPlugin.Cad.Execution;

public sealed class CadTransactionScope : IDisposable
{
    private readonly Document _document;
    private readonly DocumentLock? _documentLock;
    private readonly Transaction _transaction;
    private bool _committed;
    private bool _disposed;

    public Document Document => _document;
    public Database Database => _document.Database;
    public Transaction Transaction => _transaction;

    public CadTransactionScope()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
            throw new InvalidOperationException("No active AutoCAD drawing document was found.");
        }

        _document = doc;
        _documentLock = _document.LockDocument();
        _transaction = _document.TransactionManager.StartTransaction();
    }

    public BlockTableRecord GetCurrentSpace(OpenMode openMode = OpenMode.ForRead)
    {
        return (BlockTableRecord)_transaction.GetObject(Database.CurrentSpaceId, openMode);
    }

    public BlockTableRecord GetModelSpace(OpenMode openMode = OpenMode.ForRead)
    {
        var blockTable = (BlockTable)_transaction.GetObject(Database.BlockTableId, OpenMode.ForRead);
        return (BlockTableRecord)_transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], openMode);
    }

    public void Commit()
    {
        if (!_committed && !_disposed)
        {
            _transaction.Commit();
            _committed = true;
        }
    }

    public void Abort()
    {
        if (!_committed && !_disposed)
        {
            _transaction.Abort();
            _committed = false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (!_committed)
            {
                _transaction.Abort();
            }
            _transaction.Dispose();
            _documentLock?.Dispose();
            _disposed = true;
        }
    }
}
