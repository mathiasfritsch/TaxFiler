import { test, expect } from '@playwright/test';

test.describe('Transactions Page', () => {
  const testYearMonth = '2026-01';
  const transactionsUrl = `/transactions/${testYearMonth}`;

  // Mock transaction data
  const mockTransactions = [
    {
      id: 1,
      netAmount: 1000.00,
      grossAmount: 1190.00,
      senderReceiver: "Müller GmbH & Co. KG",
      transactionNote: "Rechnung R-2026-001 - Beratungsleistungen",
      accountName: "Geschäftskonto Sparkasse",
      documentName: "Rechnung_Müller_R-2026-001.pdf",
      taxAmount: 190.00,
      transactionDateTime: "2026-01-15T09:30:00",
      isSalesTaxRelevant: true,
      isIncomeTaxRelevant: true,
      documentId: 101,
      accountId: 1
    },
    {
      id: 2,
      netAmount: 250.50,
      grossAmount: 298.10,
      senderReceiver: "Büroausstattung Schmidt",
      transactionNote: "Büromaterial - Bestellung BS-456",
      accountName: "Geschäftskonto Sparkasse",
      documentName: "Beleg_Schmidt_BS-456.pdf",
      taxAmount: 47.60,
      transactionDateTime: "2026-01-12T14:22:00",
      isSalesTaxRelevant: true,
      isIncomeTaxRelevant: true,
      documentId: 102,
      accountId: 1
    },
    {
      id: 3,
      netAmount: -75.00,
      grossAmount: -89.25,
      senderReceiver: "Tankstelle Aral",
      transactionNote: "Kraftstoff - Geschäftsfahrt München",
      accountName: "Geschäftskonto Sparkasse",
      documentName: null,
      taxAmount: -14.25,
      transactionDateTime: "2026-01-10T16:45:00",
      isSalesTaxRelevant: true,
      isIncomeTaxRelevant: true,
      documentId: null,
      accountId: 1
    }
  ];

  test.beforeEach(async ({ page }) => {
    // Mock the API response with test data
    await page.route('**/api/transactions/gettransactions*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockTransactions)
      });
    });

    // Navigate to the transactions page
    await page.goto(transactionsUrl);
  });


  test('should display correct number of transactions from test data', async ({ page }) => {
    // Wait for the grid to load
    await expect(page.locator('[role="grid"]')).toBeVisible();
    
    // Take a screenshot after the page loads
    await page.screenshot({ path: 'test-results/transactions-page-loaded.png', fullPage: true });
    
    // Count the actual number of transactions in our mock data
    const testDataCount = mockTransactions.length;
    
    // Verify grid shows exactly that many data rows (plus header)
    const gridRows = page.locator('[role="grid"] [role="row"]');
    await expect(gridRows).toHaveCount(testDataCount + 1); // +1 for header row
    
    // Verify specific transaction data is displayed
    await expect(page.locator('text=Müller GmbH & Co. KG')).toBeVisible();
    await expect(page.locator('text=Büroausstattung Schmidt')).toBeVisible();
    await expect(page.locator('text=Tankstelle Aral')).toBeVisible();
    
    // Take a screenshot showing the populated grid
    await page.screenshot({ path: 'test-results/transactions-grid-populated.png', fullPage: true });
    
    // Check that Edit and Delete buttons match transaction count
    const editButtons = page.locator('button:has-text("Edit")');
    const deleteButtons = page.locator('button:has-text("Delete")');
    
    await expect(editButtons).toHaveCount(testDataCount);
    await expect(deleteButtons).toHaveCount(testDataCount);
    
    // Take a final screenshot for the report
    await page.screenshot({ path: 'test-results/transactions-final-state.png', fullPage: true });
    
    // Log for debugging
    console.log(`Expected transactions: ${testDataCount}, Mock data length: ${mockTransactions.length}`);
  });
});