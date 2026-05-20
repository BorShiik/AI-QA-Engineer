# **AI QA Engineer: Automated Code Review & Unit Test Generator**

An advanced AI-powered agent designed to automate software quality assurance workflows. This tool reads local source files, conducts a rigorous technical code review, and automatically synthesizes standalone unit testing scripts. Built natively in Python, it integrates with the Google Gemini API using the state-of-the-art gemini-3.1-pro-preview model.

## **🚀 Features**

* **Automated Code Review**: Analyzes algorithmic complexity, reveals hidden edge-case bugs, and enforces structural clean-code best practices.  
* **Autonomous Test Synthesis**: Generates a complete, functional test suite targeting both happy paths and boundary conditions.  
* **Multi-Language Support**: Automatically detects the source file language (Python, C\#, TypeScript, etc.) and utilizes the appropriate testing framework (pytest, xUnit, Jest).  
* **Secure Configuration**: Uses .env files for secure API key management, preventing credential leaks.  
* **Autonomous File Writing**: Seamlessly extracts code snippets from the model's payload and creates physical test files locally on your disk.

## **📁 Project Structure**

├── .env               \# (Not tracked by Git) Your secure API keys  
├── .gitignore         \# Tells Git to ignore .env and cache files  
├── ai\_qa\_agent.py     \# Main Python script hosting the AI Agent pipeline  
├── README.md          \# Project documentation and setup guide  
└── target\_file.py     \# Your source code file to analyze (e.g., prANS480.py)

## **🛠️ Installation & Setup**

1. **Clone the Repository**  
   git clone \[https://github.com/BorShiik/AI-QA-Engineer.git\](https://github.com/BorShiik/AI-QA-Engineer.git)  
   cd ai-qa-engineer

2. **Install Dependencies**  
   The project requires the requests library for HTTP calls and python-dotenv for secure environment variable management:  
   pip install requests python-dotenv

3. **Configure Environment Variables**  
   * Obtain an API key from [Google AI Studio](https://aistudio.google.com/).  
   * Create a file named .env in the root directory of the project.  
   * Add your API key to the .env file like this:  
     GEMINI\_API\_KEY=your\_actual\_api\_key\_here

## **💻 Usage**

To run the agent against a specific source code file, run the script via terminal and pass the file path as an argument:

python ai\_qa\_agent.py path/to/your/code.py

### **Example Context**

If you execute the agent against a Traveling Salesperson Problem (TSP) script such as prANS480.py:

1. The terminal prints a detailed, itemized Code Review in Polish.  
2. A brand-new testing script named test\_prANS480.py is automatically compiled and saved in the identical directory.

## **📄 License**

Distributed under the MIT License. See LICENSE for more information.
