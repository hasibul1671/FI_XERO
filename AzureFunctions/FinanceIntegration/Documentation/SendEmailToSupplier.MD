This is a mermaid flow to represent how the AzureFunction FinanceIntegration works. This is not completed. Must be completed over time and as we fix/develop new features.


The mermaid diagram represents the purchase order send to supplier.
::: mermaid
 graph TD;
A[ServiceBusTrigger financedelegator is called];
B[PurchaseOrder class instantiated];
C{Status: Awaiting Delivery};
D{Status: Sent to Supplier/Sent to Xero - Updated};
E[Create purchaseorder queue item];
F[PurchaseOrderGeneration Function listens to the queue];
G[Method Process is called];
H[Get configurations from table FinanceConfiguration in Azure];
I[Download the PDF via HTTP request];
J[Initialize the template email];
K[Set Parties TO and FROM];
L[Create email];
M[Attach the PDF];
N[Attempt to send];
O[Create Xero queue item];

A --> B;
B --> C;
C --> E;
E --> F;
F --> G;
G --> H;
H --> I;
I --> J;
J --> K;
K --> L;
L --> M;
M --> N;
B --> D;
D --> O;

:::


