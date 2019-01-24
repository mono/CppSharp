#pragma once

#include <qabstractitemmodel.h>
#include "AstReader.h"

namespace Qt
{
int const NodeRole = UserRole + 1;
}

Q_DECLARE_METATYPE(GenericAstNode*)

class AstModel : public QAbstractItemModel
{
    Q_OBJECT

public:
    explicit AstModel(GenericAstNode *data, QObject *parent = 0);
    ~AstModel();

    QVariant data(const QModelIndex &index, int role) const override;
    Qt::ItemFlags flags(const QModelIndex &index) const override;
    QVariant headerData(int section, Qt::Orientation orientation, int role = Qt::DisplayRole) const override;
    QModelIndex index(int row, int column, const QModelIndex &parent = QModelIndex()) const override;
    QModelIndex parent(const QModelIndex &index) const override;
    int rowCount(const QModelIndex &parent = QModelIndex()) const override;
    int columnCount(const QModelIndex &parent = QModelIndex()) const override;
    QModelIndex rootIndex() const;
    bool hasChildren(const QModelIndex &parent = QModelIndex()) const override;

private:
    void setupModelData(const QStringList &lines, GenericAstNode *parent);

    GenericAstNode *rootItem;
};