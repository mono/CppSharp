#include "AstModel.h"
#include <qbrush.h>


AstModel::AstModel(GenericAstNode *data, QObject *parent): 
    QAbstractItemModel(parent),
    rootItem(data)
{
}

AstModel::~AstModel()
{
}

int AstModel::columnCount(const QModelIndex &parent) const
{
    return 1;
}

QVariant AstModel::data(const QModelIndex &index, int role) const
{
    if (!index.isValid())
        return QVariant();

    if (role != Qt::DisplayRole && role != Qt::ForegroundRole && role != Qt::NodeRole)
        return QVariant();

    auto item = static_cast<GenericAstNode*>(index.internalPointer());
    switch (role)
    {
    case Qt::DisplayRole:
        return QVariant(QString::fromStdString(item->name));
    case Qt::ForegroundRole:
        switch (item->getColor())
        {
        case 0:
            return QVariant(QBrush(Qt::GlobalColor::darkBlue));
        case 1:
            return QVariant(QBrush(Qt::GlobalColor::darkGreen));
        default:
            return QVariant(QBrush(Qt::GlobalColor::black));
        }
    case Qt::NodeRole:
        return QVariant::fromValue(item);
    }
    return QVariant(QString::fromStdString(item->name));
}

Qt::ItemFlags AstModel::flags(const QModelIndex &index) const
{
    if (!index.isValid())
        return 0;

    return QAbstractItemModel::flags(index);
}

QVariant AstModel::headerData(int section, Qt::Orientation orientation, int role) const
{
    if (orientation == Qt::Horizontal && role == Qt::DisplayRole)
        return QVariant("Test");

    return QVariant();
}

QModelIndex AstModel::index(int row, int column, const QModelIndex &parent) const
{
    if (!hasIndex(row, column, parent))
        return QModelIndex();


    if (!parent.isValid())
    {
        return rootIndex();
    }

    auto parentItem = static_cast<GenericAstNode*>(parent.internalPointer());
    auto &childItem = parentItem->myChidren[row];
    if (childItem)
        return createIndex(row, column, childItem.get());
    else
        return QModelIndex();
}

QModelIndex AstModel::rootIndex() const
{
    return createIndex(0, 0, rootItem);
}

QModelIndex AstModel::parent(const QModelIndex &index) const
{
    if (!index.isValid())
        return QModelIndex();

    GenericAstNode *childItem = static_cast<GenericAstNode*>(index.internalPointer());
    if (childItem == rootItem || childItem->myParent == nullptr)
        return QModelIndex();

    GenericAstNode *parentItem = childItem->myParent;

    if (parentItem == rootItem)
        return rootIndex();
    auto grandFather = parentItem->myParent;
    auto parentRow = grandFather == nullptr ?
        0 :
        grandFather->findChildIndex(parentItem);

    return createIndex(parentRow, 0, parentItem);
}

int AstModel::rowCount(const QModelIndex &parent) const
{
    GenericAstNode *parentItem;
    if (parent.column() > 0)
        return 0;

    if (parent.isValid())
    {
        parentItem = static_cast<GenericAstNode*>(parent.internalPointer());
        return parentItem->myChidren.size();
    }
    else
    {
        return 1;
    }
}

bool AstModel::hasChildren(const QModelIndex &parent) const
{
    GenericAstNode *parentItem;
    if (parent.column() > 0)
        return false;

    if (parent.isValid())
        parentItem = static_cast<GenericAstNode*>(parent.internalPointer());
    else
        parentItem = rootItem;

    return !parentItem->myChidren.empty();

}

