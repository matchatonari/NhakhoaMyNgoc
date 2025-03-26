BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "Customer" (
	"Customer_IsActive"	INTEGER NOT NULL DEFAULT 1,
	"Customer_FullName"	TEXT NOT NULL COLLATE NOCASE,
	"Customer_Sex"	INTEGER NOT NULL DEFAULT 0,
	"Customer_Birthdate"	TEXT NOT NULL,
	"Customer_Id"	INTEGER NOT NULL,
	"Customer_Address"	TEXT NOT NULL,
	"Customer_Phone"	TEXT,
	"Customer_CitizenId"	TEXT,
	PRIMARY KEY("Customer_Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "Expense" (
	"Expense_Id"	INTEGER,
	"Expense_Date"	TEXT,
	"Expense_IsInput"	INTEGER,
	"Expense_Participant"	TEXT,
	"Expense_Address"	TEXT,
	"Expense_Content"	TEXT,
	"Expense_Amount"	INTEGER,
	"Expense_CertificateId"	INTEGER,
	PRIMARY KEY("Expense_Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "Receipt" (
	"Receipt_Id"	INTEGER NOT NULL,
	"Receipt_CustomerId"	TEXT NOT NULL,
	"Receipt_Date"	TEXT NOT NULL,
	"Receipt_Total"	INTEGER NOT NULL,
	"Receipt_Remaining"	INTEGER NOT NULL,
	"Receipt_RevisitDate"	TEXT,
	"Receipt_Notes"	TEXT,
	PRIMARY KEY("Receipt_Id" AUTOINCREMENT),
	FOREIGN KEY("Receipt_CustomerId") REFERENCES "Customer"("Customer_Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "ReceiptDetail" (
	"ReceiptDetail_Id"	INTEGER NOT NULL,
	"ReceiptDetail_ReceiptId"	INTEGER NOT NULL,
	"ReceiptDetail_Content"	TEXT NOT NULL,
	"ReceiptDetail_Quantity"	INTEGER NOT NULL,
	"ReceiptDetail_Price"	INTEGER NOT NULL,
	"ReceiptDetail_Discount"	INTEGER NOT NULL,
	"ReceiptDetail_Total"	 GENERATED ALWAYS AS ("ReceiptDetail_Quantity" * "ReceiptDetail_Price" - "ReceiptDetail_Discount") STORED,
	PRIMARY KEY("ReceiptDetail_Id" AUTOINCREMENT),
	FOREIGN KEY("ReceiptDetail_ReceiptID") REFERENCES "Receipt"("Receipt_Id") ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS "Stock" (
	"Stock_IsActive"	INTEGER DEFAULT 1,
	"Stock_Id"	INTEGER,
	"Stock_Name"	TEXT,
	"Stock_Quantity"	INTEGER DEFAULT 0,
	"Stock_Total"	INTEGER DEFAULT 0,
	"Stock_Unit"	TEXT,
	PRIMARY KEY("Stock_Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "StockList" (
	"StockList_Id"	INTEGER,
	"StockList_Alias"	TEXT,
	"StockList_Address"	TEXT,
	"StockList_IsActive"	INTEGER NOT NULL DEFAULT 1,
	PRIMARY KEY("StockList_Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "StockReceipt" (
	"StockReceipt_Id"	INTEGER NOT NULL,
	"StockReceipt_Date"	TEXT NOT NULL,
	"StockReceipt_Correspondent"	TEXT NOT NULL,
	"StockReceipt_Division"	TEXT,
	"StockReceipt_Reason"	TEXT NOT NULL,
	"StockReceipt_StockId"	INTEGER NOT NULL,
	"StockReceipt_CertificateId"	INTEGER NOT NULL,
	"StockReceipt_Total"	INTEGER NOT NULL,
	"StockReceipt_IsInput"	INTEGER NOT NULL DEFAULT 1,
	PRIMARY KEY("StockReceipt_Id" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "StockReceiptDetail" (
	"StockReceiptDetail_Id"	INTEGER,
	"StockReceiptDetail_ReceiptID"	INTEGER NOT NULL,
	"StockReceiptDetail_ItemId"	INTEGER NOT NULL,
	"StockReceiptDetail_Unit"	TEXT NOT NULL,
	"StockReceiptDetail_Quantity"	INTEGER NOT NULL,
	"StockReceiptDetail_Demand"	INTEGER NOT NULL,
	"StockReceiptDetail_Price"	INTEGER NOT NULL,
	"StockReceiptDetail_Total"	 GENERATED ALWAYS AS ("StockReceiptDetail_Quantity" * "StockReceiptDetail_Price") STORED,
	PRIMARY KEY("StockReceiptDetail_Id" AUTOINCREMENT),
	FOREIGN KEY("StockReceiptDetail_ReceiptID") REFERENCES "StockReceipt"("StockReceipt_Id") ON DELETE CASCADE
);
CREATE TRIGGER UpdateReceiptAfterDelete
AFTER DELETE ON ReceiptDetail
FOR EACH ROW
BEGIN
    UPDATE Receipt
    SET Receipt_Total = (SELECT SUM(ReceiptDetail_Total) 
                              FROM ReceiptDetail 
                              WHERE ReceiptDetail_ReceiptId = OLD.ReceiptDetail_ReceiptId)
    WHERE Receipt_Id = NEW.ReceiptDetail_ReceiptId;
END;
CREATE TRIGGER UpdateReceiptAfterInsert
AFTER INSERT ON ReceiptDetail
FOR EACH ROW
BEGIN
    -- Nếu đã tồn tại, cập nhật lại tổng tiền
    UPDATE Receipt
    SET Receipt_Total = (
        SELECT SUM(ReceiptDetail_Total)
        FROM ReceiptDetail
        WHERE ReceiptDetail_ReceiptId = NEW.ReceiptDetail_ReceiptId
    )
    WHERE Receipt_Id = NEW.ReceiptDetail_ReceiptId;
END;
CREATE TRIGGER UpdateReceiptAfterUpdate
AFTER UPDATE ON ReceiptDetail
FOR EACH ROW
BEGIN
    UPDATE Receipt
    SET Receipt_Total = (SELECT SUM(ReceiptDetail_Total) 
                              FROM ReceiptDetail 
                              WHERE ReceiptDetail_ReceiptId = NEW.ReceiptDetail_ReceiptId)
    WHERE Receipt_Id = NEW.ReceiptDetail_ReceiptId;
END;
CREATE TRIGGER UpdateStockAfterDelete
AFTER DELETE ON StockReceiptDetail
FOR EACH ROW
BEGIN
    UPDATE Stock 
    SET 
        Stock_Quantity = COALESCE((
            SELECT SUM(StockReceiptDetail_Quantity * 
                (CASE WHEN StockReceipt.StockReceipt_IsInput = 1 THEN 1 ELSE -1 END))
            FROM StockReceiptDetail 
            JOIN StockReceipt ON StockReceipt.StockReceipt_Id = StockReceiptDetail.StockReceiptDetail_ReceiptID
            WHERE StockReceiptDetail_ItemId = OLD.StockReceiptDetail_ItemId
        ), 0), -- Nếu không còn hàng nào thì gán 0

        Stock_Total = COALESCE((
            SELECT SUM(StockReceiptDetail_Quantity * StockReceiptDetail_Price * 
                (CASE WHEN StockReceipt.StockReceipt_IsInput = 1 THEN 1 ELSE -1 END))
            FROM StockReceiptDetail 
            JOIN StockReceipt ON StockReceipt.StockReceipt_Id = StockReceiptDetail.StockReceiptDetail_ReceiptID
            WHERE StockReceiptDetail_ItemId = OLD.StockReceiptDetail_ItemId
        ), 0) -- Nếu không còn hàng nào thì gán 0

    WHERE Stock_Id = OLD.StockReceiptDetail_ItemId;
END;
CREATE TRIGGER UpdateStockAfterInsert
AFTER INSERT ON StockReceiptDetail
FOR EACH ROW
BEGIN
    UPDATE Stock 
    SET Stock_Quantity = Stock_Quantity + NEW.StockReceiptDetail_Quantity * 
        (SELECT CASE WHEN StockReceipt_IsInput = 1 THEN 1 ELSE -1 END 
         FROM StockReceipt 
         WHERE StockReceipt_Id = NEW.StockReceiptDetail_ReceiptId),
        
        Stock_Total = Stock_Total + NEW.StockReceiptDetail_Quantity * NEW.StockReceiptDetail_Price * 
        (SELECT CASE WHEN StockReceipt_IsInput = 1 THEN 1 ELSE -1 END 
         FROM StockReceipt 
         WHERE StockReceipt_Id = NEW.StockReceiptDetail_ReceiptId)
        
    WHERE Stock_Id = NEW.StockReceiptDetail_ItemId;
END;
CREATE TRIGGER UpdateStockAfterUpdate
AFTER UPDATE ON StockReceiptDetail
FOR EACH ROW
BEGIN
    -- Giảm số lượng của vật phẩm cũ
    UPDATE Stock 
    SET Stock_Quantity = Stock_Quantity - OLD.StockReceiptDetail_Quantity * 
        (SELECT CASE WHEN StockReceipt_IsInput = 1 THEN 1 ELSE -1 END 
         FROM StockReceipt 
         WHERE StockReceipt_Id = OLD.StockReceiptDetail_ReceiptId),

        Stock_Total = Stock_Total - OLD.StockReceiptDetail_Quantity * OLD.StockReceiptDetail_Price * 
        (SELECT CASE WHEN StockReceipt_IsInput = 1 THEN 1 ELSE -1 END 
         FROM StockReceipt 
         WHERE StockReceipt_Id = OLD.StockReceiptDetail_ReceiptId)
        
    WHERE Stock_Id = OLD.StockReceiptDetail_ItemId;

    -- Tăng số lượng của vật phẩm mới
    UPDATE Stock 
    SET Stock_Quantity = Stock_Quantity + NEW.StockReceiptDetail_Quantity * 
        (SELECT CASE WHEN StockReceipt_IsInput = 1 THEN 1 ELSE -1 END 
         FROM StockReceipt 
         WHERE StockReceipt_Id = NEW.StockReceiptDetail_ReceiptId),

        Stock_Total = Stock_Total + NEW.StockReceiptDetail_Quantity * NEW.StockReceiptDetail_Price * 
        (SELECT CASE WHEN StockReceipt_IsInput = 1 THEN 1 ELSE -1 END 
         FROM StockReceipt 
         WHERE StockReceipt_Id = NEW.StockReceiptDetail_ReceiptId)
        
    WHERE Stock_Id = NEW.StockReceiptDetail_ItemId;
END;
CREATE TRIGGER UpdateStockReceiptAfterDelete
AFTER DELETE ON StockReceiptDetail
FOR EACH ROW
BEGIN
    UPDATE StockReceipt
    SET StockReceipt_Total = (SELECT SUM(StockReceiptDetail_Total) 
                              FROM StockReceiptDetail 
                              WHERE StockReceiptDetail_ReceiptId = OLD.StockReceiptDetail_ReceiptId)
    WHERE StockReceipt_Id = OLD.StockReceiptDetail_ReceiptId;
END;
CREATE TRIGGER UpdateStockReceiptAfterInsert
AFTER INSERT ON StockReceiptDetail
FOR EACH ROW
BEGIN
    -- Nếu đã tồn tại, cập nhật lại tổng tiền
    UPDATE StockReceipt
    SET StockReceipt_Total = (
        SELECT SUM(StockReceiptDetail_Total)
        FROM StockReceiptDetail
        WHERE StockReceiptDetail_ReceiptId = NEW.StockReceiptDetail_ReceiptId
    )
    WHERE StockReceipt_Id = NEW.StockReceiptDetail_ReceiptId;
END;
CREATE TRIGGER UpdateStockReceiptAfterUpdate
AFTER UPDATE ON StockReceiptDetail
FOR EACH ROW
BEGIN
    UPDATE StockReceipt
    SET StockReceipt_Total = (SELECT SUM(StockReceiptDetail_Total) 
                              FROM StockReceiptDetail 
                              WHERE StockReceiptDetail_ReceiptId = NEW.StockReceiptDetail_ReceiptId)
    WHERE StockReceipt_Id = NEW.StockReceiptDetail_ReceiptId;
END;
CREATE TRIGGER trg_UpdateStockOnIsInputChange
AFTER UPDATE OF StockReceipt_IsInput
ON StockReceipt
FOR EACH ROW
WHEN OLD.StockReceipt_IsInput != NEW.StockReceipt_IsInput
BEGIN
    -- Ghi log khi trigger chạy
    INSERT INTO Debug (Message)
    VALUES ('Trigger Fired: StockReceipt_Id=' || NEW.StockReceipt_Id || 
            ', IsInput=' || NEW.StockReceipt_IsInput);

    -- Ghi log từng sản phẩm bị ảnh hưởng
    INSERT INTO Debug (Message)
    SELECT 'Stock_Id=' || Stock.Stock_Id || 
           ', Old_Quantity=' || Stock.Stock_Quantity || 
           ', ReceiptDetail_Quantity=' || StockReceiptDetail.StockReceiptDetail_Quantity || 
           ', IsInput=' || NEW.StockReceipt_IsInput
    FROM Stock
    JOIN StockReceiptDetail 
        ON StockReceiptDetail.StockReceiptDetail_ItemId = Stock.Stock_Id
    WHERE StockReceiptDetail.StockReceiptDetail_ReceiptID = NEW.StockReceipt_Id;

    -- Cập nhật số lượng hàng tồn kho
    UPDATE Stock
    SET Stock_Quantity = Stock_Quantity + 
        ((CASE WHEN NEW.StockReceipt_IsInput = 1 THEN 1 ELSE -1 END) * 
        2 * COALESCE((
            SELECT StockReceiptDetail_Quantity
            FROM StockReceiptDetail
            WHERE StockReceiptDetail_ReceiptID = NEW.StockReceipt_Id
            AND StockReceiptDetail_ItemId = Stock.Stock_Id
        ), 0))
    WHERE EXISTS (
        SELECT 1 FROM StockReceiptDetail
        WHERE StockReceiptDetail_ReceiptID = NEW.StockReceipt_Id
        AND StockReceiptDetail_ItemId = Stock.Stock_Id
    );

    -- Ghi log sau khi cập nhật
    INSERT INTO Debug (Message)
    SELECT 'Updated Stock_Id=' || Stock.Stock_Id || 
           ', New_Quantity=' || Stock.Stock_Quantity
    FROM Stock
    WHERE EXISTS (
        SELECT 1 FROM StockReceiptDetail
        WHERE StockReceiptDetail_ReceiptID = NEW.StockReceipt_Id
        AND StockReceiptDetail_ItemId = Stock.Stock_Id
    );
END;
COMMIT;
