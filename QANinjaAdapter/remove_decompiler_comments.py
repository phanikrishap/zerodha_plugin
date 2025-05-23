import os
import re

def remove_decompiler_comments(file_path):
    """Remove JetBrains decompiler comments from a C# file."""
    try:
        # Read file with different encodings to handle various file formats
        content = None
        encodings = ['utf-8', 'utf-8-sig', 'latin-1', 'cp1252']
        
        for encoding in encodings:
            try:
                with open(file_path, 'r', encoding=encoding) as f:
                    content = f.read()
                break
            except UnicodeDecodeError:
                continue
        
        if content is None:
            print(f"Could not read file {file_path} with any encoding")
            return False
        
        # Check if file contains decompiler comments (case insensitive and flexible)
        if not re.search(r'//\s*Decompiled\s+with\s+JetBrains\s+decompiler', content, re.IGNORECASE):
            return False
        
        print(f"Processing: {file_path}")
        
        lines = content.splitlines()
        new_lines = []
        skip_lines = False
        found_decompiler_comment = False
        
        i = 0
        while i < len(lines):
            line = lines[i].strip()
            
            # Check if this is the start of decompiler comments (flexible matching)
            if re.match(r'//\s*Decompiled\s+with\s+JetBrains\s+decompiler', line, re.IGNORECASE):
                found_decompiler_comment = True
                skip_lines = True
                i += 1
                continue
            
            # Skip lines that are part of the decompiler comment block
            if skip_lines:
                # Check for various decompiler comment patterns
                if (re.match(r'//\s*Type:', line, re.IGNORECASE) or 
                    re.match(r'//\s*Assembly:', line, re.IGNORECASE) or 
                    re.match(r'//\s*MVID:', line, re.IGNORECASE) or 
                    re.match(r'//\s*Assembly\s+location:', line, re.IGNORECASE) or 
                    line == '//' or
                    line == ''):
                    i += 1
                    continue
                else:
                    # We've reached the end of the comment block
                    skip_lines = False
                    # Don't skip this line, process it normally
            
            # Add the line if we're not skipping
            if not skip_lines:
                new_lines.append(lines[i])
            
            i += 1
        
        if found_decompiler_comment:
            # Remove leading empty lines
            while new_lines and new_lines[0].strip() == '':
                new_lines.pop(0)
            
            # Join lines and write back to file
            new_content = '\n'.join(new_lines)
            
            # Write back with UTF-8 encoding
            with open(file_path, 'w', encoding='utf-8', newline='') as f:
                f.write(new_content)
            
            print(f"  ✓ Removed decompiler comments from {file_path}")
            return True
        
        return False
    
    except Exception as e:
        print(f"Error processing {file_path}: {e}")
        return False

def main():
    print("Removing JetBrains decompiler comments from C# files...")
    print("=" * 60)
    
    total_files = 0
    modified_files = 0
    
    # Walk through all directories and find C# files
    for root, dirs, files in os.walk('.'):
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                total_files += 1
                
                if remove_decompiler_comments(file_path):
                    modified_files += 1
    
    print("=" * 60)
    print(f"Summary:")
    print(f"  Total C# files: {total_files}")
    print(f"  Files modified: {modified_files}")
    print(f"  Files unchanged: {total_files - modified_files}")
    
    if modified_files > 0:
        print(f"\n✓ Successfully removed JetBrains decompiler comments from {modified_files} files!")
    else:
        print("\n! No files needed modification.")

if __name__ == "__main__":
    main()
