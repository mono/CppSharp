#pragma once

#include "ui_MainWindow.h"
#include "Highlighter.h"
#include "AstReader.h"


class MainWindow : public QMainWindow
{
    Q_OBJECT
public:
    MainWindow(QWidget *parent = nullptr);
public slots:
    void RefreshAst();
    void HighlightCodeMatchingNode(const QModelIndex &newNode, const QModelIndex &previousNode);
    void DisplayNodeProperties(const QModelIndex &newNode, const QModelIndex &previousNode);
    void HighlightNodeMatchingCode();
    void ShowNodeDetails();
    void OnCodeChange();
    void closeEvent(QCloseEvent *event) override;
private:
    Ui::MainWindow myUi;
    Highlighter *myHighlighter; // No need to delete, since is will have a parent that will take care of that
    AstReader myReader;
    std::vector<QDialog *> myDetailWindows;
    bool isUpdateInProgress;
};
