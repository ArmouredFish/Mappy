﻿namespace Mappy.Models.Session
{
    public interface ISelectionCommandHandler
    {
        bool IsInSelection(int x, int y);

        bool SelectAtPoint(int x, int y);

        void ClearSelection();

        void DeleteSelection();

        void TranslateSelection(int x, int y);

        void FlushTranslation();

        void DragDropFeature(string name, int x, int y);

        void DragDropTile(int id, int x, int y);

        void DragDropStartPosition(int index, int x, int y);

        void StartBandbox(int x, int y);

        void GrowBandbox(int x, int y);

        void CommitBandbox();

        void SelectTile(int index);
    }
}
